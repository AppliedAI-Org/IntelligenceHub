using Azure.AI.OpenAI;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Extensions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    public class AGIClient : IAGIClient
    {
        private AzureOpenAIClient _azureOpenAIClient;

        public AGIClient(IOptionsMonitor<AGIClientSettings> settings, IHttpClientFactory policyFactory)
        {
            var policyClient = policyFactory.CreateClient(ClientPolicy.CompletionClient.ToString());

            var service = settings.CurrentValue.Services.Find(service => service.Endpoint == policyClient.BaseAddress?.ToString())
                ?? throw new InvalidOperationException("service key failed to be retrieved when attempting to generate a completion.");

            var apiKey = service.Key;
            var credential = new ApiKeyCredential(apiKey);
            var options = new AzureOpenAIClientOptions()
            {
                Transport = new HttpClientPipelineTransport(policyClient)
            };
            _azureOpenAIClient = new AzureOpenAIClient(policyClient.BaseAddress, credential, options);  //+ "chat/completions"; add this if url is for OpenAI instead of Azure OpenAI
        }

        public async Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(completionRequest.ProfileOptions.Name) || completionRequest.Messages.Count < 1) return new CompletionResponse() { FinishReason = FinishReason.Error };
                var options = BuildCompletionOptions(completionRequest);
                var messages = BuildCompletionMessages(completionRequest);
                var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);
                var completionResult = await chatClient.CompleteChatAsync(messages, options);

                var toolCalls = new Dictionary<string, string>();
                foreach (var tool in completionResult.Value.ToolCalls) toolCalls.Add(tool.FunctionName, tool.FunctionArguments.ToString());

                // build the response object
                var responseMessage = new Message()
                {
                    Content = completionResult.Value.Content[0].Text ?? string.Empty,
                    Role = completionResult.Value.Role.ToString().ConvertStringToRole(),
                    TimeStamp = DateTime.UtcNow
                };

                foreach (var content in completionResult.Value.Content)
                {
                    if (responseMessage.Base64Image == null && content.Kind == ChatMessageContentPartKind.Image) responseMessage.Base64Image = Convert.ToBase64String(content.ImageBytes);
                    else if (string.IsNullOrEmpty(responseMessage.Content) && content.Kind == ChatMessageContentPartKind.Text) responseMessage.Content = content.Text;
                }

                var response = new CompletionResponse()
                {
                    FinishReason = completionResult.Value.FinishReason.ToString().ConvertStringToFinishReason(),
                    Messages = completionRequest.Messages,
                    ToolCalls = toolCalls
                };
                response.Messages.Add(responseMessage);
                return response ?? new CompletionResponse() { FinishReason = FinishReason.Error };
            }
            catch (Exception)
            {
                return new CompletionResponse() { FinishReason = FinishReason.Error };
            }

        }

        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);
            var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);
            var resultCollection = chatClient.CompleteChatStreamingAsync(messages, options);

            var chunkId = 0;
            string role = null;
            string finishReason = null;
            var currentTool = string.Empty;
            var currentToolArgs = string.Empty;
            var toolCalls = new Dictionary<string, string>();
            await foreach (var result in resultCollection)
            {
                if (!string.IsNullOrEmpty(result.Role.ToString())) role = result.Role.ToString() ?? role;
                if (!string.IsNullOrEmpty(result.FinishReason.ToString())) finishReason = result.FinishReason.ToString() ?? finishReason;
                var content = string.Empty;
                var base64Image = string.Empty;

                foreach (var update in result.ContentUpdate)
                {
                    if (string.IsNullOrEmpty(base64Image) && update.Kind == ChatMessageContentPartKind.Image) base64Image = Convert.ToBase64String(update.ImageBytes);
                    if (string.IsNullOrEmpty(content) && update.Kind == ChatMessageContentPartKind.Text) content += update.Text;
                }

                // handle tool conversion - move to seperate method
                foreach (var update in result.ToolCallUpdates)
                {
                    // capture current values
                    if (string.IsNullOrEmpty(currentTool)) currentTool = update.FunctionName;
                    if (currentTool == update.FunctionName) currentToolArgs += update.FunctionArgumentsUpdate.ToString();
                    else
                    {
                        currentTool = update.FunctionName;
                        currentToolArgs = update.FunctionArgumentsUpdate.ToString();
                    }

                    if (toolCalls.ContainsKey(currentTool)) toolCalls[currentTool] = currentToolArgs;
                    else toolCalls.Add(currentTool, currentToolArgs);
                }

                yield return new CompletionStreamChunk()
                {
                    Id = chunkId++,
                    Role = role?.ConvertStringToRole(),
                    CompletionUpdate = content,
                    Base64Image = base64Image,
                    FinishReason = finishReason?.ConvertStringToFinishReason(),
                    ToolCalls = toolCalls
                };
            }
        }

        private List<ChatMessage> BuildCompletionMessages(CompletionRequest completionRequest)
        {
            var systemMessage = completionRequest.ProfileOptions.System_Message;
            var completionMessages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(systemMessage)) completionMessages.Add(new SystemChatMessage(systemMessage));
            foreach (var message in completionRequest.Messages)
            {
                if (message.Role.ToString() == Role.User.ToString())
                {
                    completionMessages.Add(new UserChatMessage(message.Content));

                    // Add an image if necessary
                    if (!string.IsNullOrEmpty(message.Base64Image)) completionMessages.Add(new UserChatMessage(message.Base64Image));


                    // might need to do something like this to get the above to work
                    //
                    // $"data:image/jpeg;base64,{encodedImage}"

                }
                else if (message.Role.ToString() == Role.Assistant.ToString()) completionMessages.Add(new AssistantChatMessage(message.Content));
            }
            return completionMessages;
        }

        private ChatCompletionOptions BuildCompletionOptions(CompletionRequest completion)
        {
            var options = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = completion.ProfileOptions.Max_Tokens,
                Temperature = completion.ProfileOptions.Temperature,
                TopP = completion.ProfileOptions.Top_P,
                FrequencyPenalty = completion.ProfileOptions.Frequency_Penalty,
                PresencePenalty = completion.ProfileOptions.Presence_Penalty,
                IncludeLogProbabilities = completion.ProfileOptions.Logprobs,
                EndUserId = completion.ProfileOptions.User,
            };

            // Potentially useful later for testing, validation, and fine tuning. Maps token probabilities
            //options.LogitBiases

            // set response format
            if (completion.ProfileOptions.Response_Format == ResponseFormat.Json.ToString()) options.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();
            else if (completion.ProfileOptions.Response_Format == ResponseFormat.Text.ToString()) options.ResponseFormat = ChatResponseFormat.CreateTextFormat();

            // set log probability
            if (options.IncludeLogProbabilities == true) options.TopLogProbabilityCount = completion.ProfileOptions.Top_Logprobs;

            // set stop messages
            if (completion.ProfileOptions.Stop != null && completion.ProfileOptions.Stop.Length > 0)
            {
                foreach (var message in completion.ProfileOptions.Stop) options.StopSequences.Add(message);
            }

            // set tools
            if (completion.ProfileOptions.Tools != null) foreach (var tool in completion.ProfileOptions.Tools)
                {
                    options.Tools.Add(ChatTool.CreateFunctionTool(
                        tool.Function.Name,
                        tool.Function.Description,
                        new BinaryData(tool.Function.Parameters)));
                };

            // Set tool choice
            if (completion.ProfileOptions.Tools != null && completion.ProfileOptions.Tools.Any())
            {
                if (completion.ProfileOptions.Tools.Count > 1) options.AllowParallelToolCalls = true;

                if (completion.ProfileOptions.Tool_Choice == null || completion.ProfileOptions.Tool_Choice == ToolExecutionRequirement.Auto.ToString()) options.ToolChoice = ChatToolChoice.CreateAutoChoice();
                else if (completion.ProfileOptions.Tool_Choice == ToolExecutionRequirement.None.ToString()) options.ToolChoice = ChatToolChoice.CreateNoneChoice();
                else if (completion.ProfileOptions.Tool_Choice == ToolExecutionRequirement.Required.ToString()) options.ToolChoice = ChatToolChoice.CreateRequiredChoice();
                else options.ToolChoice = ChatToolChoice.CreateFunctionChoice(completion.ProfileOptions.Tool_Choice);
            }
            // Tools and RAG DBs are not supported simultaneously, therefore RAG data is being attached at the business logic level via a direct query for now
            //if (!string.IsNullOrEmpty(completion.ProfileOptions.RagDatabase)) options = AttachDatabaseOptions(completion.ProfileOptions.RagDatabase, options);
            return options;
        }

        public async Task<float[]?> GetEmbeddings(string completion, string? embeddingModel = null)
        {
            embeddingModel = embeddingModel ?? DefaultEmbeddingModel;
            var embeddingClient = _azureOpenAIClient.GetEmbeddingClient(embeddingModel);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(completion);
            return embeddingResponse.Value.ToFloats().ToArray();
        }

        // below code is currently not being used since Azure OpenAI does not support tools with RAG via AI Search.
        // Therefore AI Search indexes are queried directly and the we manually attach the data to the final user request.
        private ChatCompletionOptions AttachDatabaseOptions(string indexName, ChatCompletionOptions options)
        {
            throw new NotImplementedException("This method has been deprecated in favor of attaching RAG data via direct requests to Azure AI Search Services");

            //var fieldMappings = new DataSourceFieldMappings();

            //// configure below dynamically based off of RAG database definition
            //fieldMappings.VectorFieldNames.Add("contentVector");
            //fieldMappings.VectorFieldNames.Add("titleVector");

            // get below values from database
            //options.AddDataSource(new AzureSearchChatDataSource()
            //{
            //    Endpoint = new Uri(_aiSearchServiceUrl), // retrieve from RagDB
            //    Authentication = DataSourceAuthentication.FromApiKey(_aiSearchServiceKey), // retrieve from RagDB
            //    IndexName = indexName, // create an Options property for API requests to hold this and below values
            //    InScope = false, // add to DatabaseOptions
            //    SemanticConfiguration = "semantic", // add to DatabaseOptions ?? defaultValue
            //    QueryType = "vector", // add to DatabaseOptions ?? defaultValue
            //    VectorizationSource = DataSourceVectorizer.FromDeploymentName(_embeddingModel), // add string to dbOptions
            //    FieldMappings = fieldMappings,

            //    // Add these
            //    TopNDocuments = 5, // get from databaseOptions ?? defaultValue
            //    OutputContextFlags = DataSourceOutputContexts.Citations | // probably just hard code value as this?
            //        DataSourceOutputContexts.Intent |
            //        DataSourceOutputContexts.AllRetrievedDocuments,

            //    Strictness = 4, // get from databaseOptions ?? null

            //    // not sure if we want to set this or not
            //    MaxSearchQueries = 5,



            //    // probably don't use below for now
            //    //AllowPartialResult = false,
            //    //Filter = // seems very useful
            //});

            return options;
        }
    }
}
