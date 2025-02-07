using Azure.AI.OpenAI;
using IntelligenceHub.API.API.DTOs.Tools;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Extensions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    public class AzureAIClient : IAGIClient
    {
        internal AzureOpenAIClient _azureOpenAIClient;

        public AzureAIClient(IOptionsMonitor<AGIClientSettings> settings, IHttpClientFactory policyFactory)
        {
            var policyClient = policyFactory.CreateClient(ClientPolicies.AzureAIClientPolicy.ToString());

            var service = settings.CurrentValue.AzureServices.Find(service => service.Endpoint == policyClient.BaseAddress?.ToString())
                ?? throw new InvalidOperationException("service key failed to be retrieved when attempting to generate a completion.");

            var apiKey = service.Key;
            var credential = new ApiKeyCredential(apiKey);
            var options = new AzureOpenAIClientOptions()
            {
                Transport = new HttpClientPipelineTransport(policyClient)
            };
            _azureOpenAIClient = new AzureOpenAIClient(policyClient.BaseAddress, credential, options);
        }

        // parameterless constructor for derived classes
        public AzureAIClient() { }

        public async Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest)
        {
            if (string.IsNullOrEmpty(completionRequest.ProfileOptions.Name) || completionRequest.Messages.Count < 1) return new CompletionResponse() { FinishReason = FinishReason.Error };
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);
            var chatClient = _azureOpenAIClient.GetChatClient(completionRequest.ProfileOptions.Model);

            var completionResult = await chatClient.CompleteChatAsync(messages, options);

            var toolCalls = new Dictionary<string, string>();
            foreach (var tool in completionResult.Value.ToolCalls)
            {
                if (tool.FunctionName.ToLower() != SystemTools.Recurse_ai_dialogue.ToString().ToLower()) toolCalls.Add(tool.FunctionName, tool.FunctionArguments.ToString());
                else toolCalls.Add(SystemTools.Recurse_ai_dialogue.ToString().ToLower(), string.Empty);
            }

            var contentString = GetMessageContent(completionResult.Value.Content.FirstOrDefault()?.Text, toolCalls);

            // build the response object
            var responseMessage = new Message()
            {
                Content = contentString,
                Role = completionResult.Value.Role.ToString().ConvertStringToRole() ?? Role.Assistant,
                User = completionRequest.ProfileOptions.User ?? string.Empty,
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
                if (!string.IsNullOrEmpty(result.Role.ToString())) role = result.Role.ToString() ?? role ?? string.Empty;
                if (!string.IsNullOrEmpty(result.FinishReason.ToString())) finishReason = result.FinishReason.ToString() ?? finishReason ?? string.Empty;
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
                    else
                    {
                        if (currentTool.ToLower() != SystemTools.Recurse_ai_dialogue.ToString().ToLower()) toolCalls.Add(currentTool, currentToolArgs);
                        else toolCalls.Add(SystemTools.Recurse_ai_dialogue.ToString().ToLower(), string.Empty);
                    }
                }
                var finalContentString = GetMessageContent(content, toolCalls);
                yield return new CompletionStreamChunk()
                {
                    Id = chunkId++,
                    Role = role?.ConvertStringToRole(),
                    User = completionRequest.ProfileOptions.User,
                    CompletionUpdate = finalContentString,
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
                MaxOutputTokenCount = completion.ProfileOptions.Max_Tokens ?? null,
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
            if (completion.ProfileOptions.Tools != null) 
                foreach (var tool in completion.ProfileOptions.Tools)
                {
                    var serializedParameters = JsonSerializer.Serialize(tool.Function.Parameters);
                    var newTool = ChatTool.CreateFunctionTool(tool.Function.Name, tool.Function.Description, BinaryData.FromString(serializedParameters));
                    options.Tools.Add(newTool);
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

        private string GetMessageContent(string? messageContent, Dictionary<string, string> toolCalls)
        {
            try
            {
                var content = messageContent ?? string.Empty;
                foreach (var tool in toolCalls)
                {
                    if (tool.Key.Equals(SystemTools.Recurse_ai_dialogue.ToString().ToLower())) return JsonSerializer.Deserialize<ProfileReferenceToolExecutionCall>(tool.Value)?.prompt_response ?? content;
                }
                return content;
            }
            catch (JsonException) { return string.Empty; }
            catch (NotSupportedException) { return string.Empty; }
            catch (ArgumentNullException) { return string.Empty; }
        }
    }
}
