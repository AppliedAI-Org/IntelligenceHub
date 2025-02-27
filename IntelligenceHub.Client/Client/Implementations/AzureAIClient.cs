using Azure.AI.OpenAI;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Extensions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// A client for interacting with Azure OpenAI and Azure AI services.
    /// </summary>
    public class AzureAIClient : IAGIClient
    {
        private AzureOpenAIClient _azureOpenAIClient;

        /// <summary>
        /// Initializes a new instance of the AzureAIClient class with the specified settings and policy factory.
        /// </summary>
        /// <param name="settings">The AGIClient settings used to configure this client.</param>
        /// <param name="policyFactory">The client factory used to retrieve a policy.</param>
        /// <exception cref="InvalidOperationException">Thrown if the provided settings are invalid.</exception>
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

        /// <summary>
        /// Generates an image based on the provided prompt using the Azure OpenAI client.
        /// </summary>
        /// <param name="prompt">The prompt used to generate the image.</param>
        /// <returns>A Base64 representation of the returned image, or null if the request fails.</returns>
        public async Task<string?> GenerateImage(string prompt)
        {
            var imageClient = _azureOpenAIClient.GetImageClient(DefaultImageGenModel);
            if (imageClient == null) return null;

            var options = new ImageGenerationOptions()
            {
                ResponseFormat = GeneratedImageFormat.Bytes,

                // add below to image gen system tool as arguments later potentially
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                //Style = GeneratedImageStyle.Vivid,
            };

            var completion = await imageClient.GenerateImageAsync(prompt, options);
            var base64Image = completion.Value.ImageBytes != null && completion.Value.ImageBytes.ToArray().Length > 0 ? Convert.ToBase64String(completion.Value.ImageBytes) : null;
            return base64Image;
        }

        /// <summary>
        /// Posts a completion request to the Azure OpenAI client and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The CompletionRequest request details used to generate a completion.</param>
        /// <returns>The completion response.</returns>
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
                if (tool.FunctionName.ToLower() != SystemTools.Chat_Recursion.ToString().ToLower()) toolCalls.Add(tool.FunctionName, tool.FunctionArguments.ToString());
                else toolCalls.Add(SystemTools.Chat_Recursion.ToString().ToLower(), string.Empty);
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

        /// <summary>
        /// Streams the completion results returned from a completion request.
        /// </summary>
        /// <param name="completionRequest">The CompletionRequest request details used to generate a completion.</param>
        /// <returns>An asyncronous collection of CompletionStreamChunks.</returns>
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
                        if (currentTool.ToLower() != SystemTools.Chat_Recursion.ToString().ToLower()) toolCalls.Add(currentTool, currentToolArgs);
                        else toolCalls.Add(SystemTools.Chat_Recursion.ToString().ToLower(), string.Empty);
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

        /// <summary>
        /// Builds the completion messages based on the provided completion request.
        /// </summary>
        /// <param name="completionRequest">The completion request to build the messages from.</param>
        /// <returns>A list of ChatMessages to be used when generating a completion from the Azure OpenAI client.</returns>
        private List<ChatMessage> BuildCompletionMessages(CompletionRequest completionRequest)
        {
            var systemMessage = completionRequest.ProfileOptions.SystemMessage;
            var completionMessages = new List<ChatMessage>();

            if (!string.IsNullOrWhiteSpace(systemMessage)) completionMessages.Add(new SystemChatMessage(systemMessage));
            foreach (var message in completionRequest.Messages)
            {
                if (message.Role == Role.User)
                {
                    // Create a list of content blocks for this user message.
                    // Each block is an anonymous object with a "type" key.
                    var contentBlocks = new List<ChatMessageContentPart>();

                    // If there is text content, add it as a "text" block.
                    if (!string.IsNullOrWhiteSpace(message.Content)) contentBlocks.Add(ChatMessageContentPart.CreateTextPart(message.Content));

                    // If an image is present, add it as an "image_url" block.
                    if (!string.IsNullOrEmpty(message.Base64Image))
                    {
                        // Prepend the proper data URI prefix – adjust the MIME type if needed.
                        // Convert the base64 string to a byte array
                        var imageBytes = Convert.FromBase64String(message.Base64Image);
                        var mimeType = GetMimeTypeFromBase64(message.Base64Image);
                        contentBlocks.Add(ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), mimeType));
                    }

                    // Create the user chat message with the composite content.
                    completionMessages.Add(new UserChatMessage(contentBlocks));
                }
                else if (message.Role == Role.Assistant) completionMessages.Add(new AssistantChatMessage(message.Content));
            }
            return completionMessages;
        }

        /// <summary>
        /// Builds the completion options based on the provided completion request.
        /// </summary>
        /// <param name="completion">The completion request to build the options from.</param>
        /// <returns>A ChatCompletionOptions object to be used when making a request to the Azure OpenAI client.</returns>
        private ChatCompletionOptions BuildCompletionOptions(CompletionRequest completion)
        {
            var options = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = completion.ProfileOptions.MaxTokens ?? null,
                Temperature = completion.ProfileOptions.Temperature,
                TopP = completion.ProfileOptions.TopP,
                FrequencyPenalty = completion.ProfileOptions.FrequencyPenalty,
                PresencePenalty = completion.ProfileOptions.PresencePenalty,
                IncludeLogProbabilities = completion.ProfileOptions.Logprobs,
                EndUserId = completion.ProfileOptions.User,
            };

            // Potentially useful later for testing, validation, and fine tuning. Maps token probabilities
            //options.LogitBiases

            // set response format
            if (completion.ProfileOptions.ResponseFormat == ResponseFormat.Json.ToString()) options.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();
            else if (completion.ProfileOptions.ResponseFormat == ResponseFormat.Text.ToString()) options.ResponseFormat = ChatResponseFormat.CreateTextFormat();

            // set log probability
            if (options.IncludeLogProbabilities == true) options.TopLogProbabilityCount = completion.ProfileOptions.TopLogprobs;

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

                if (completion.ProfileOptions.ToolChoice == null || completion.ProfileOptions.ToolChoice == ToolExecutionRequirement.Auto.ToString()) options.ToolChoice = ChatToolChoice.CreateAutoChoice();
                else if (completion.ProfileOptions.ToolChoice == ToolExecutionRequirement.None.ToString()) options.ToolChoice = ChatToolChoice.CreateNoneChoice();
                else if (completion.ProfileOptions.ToolChoice == ToolExecutionRequirement.Required.ToString()) options.ToolChoice = ChatToolChoice.CreateRequiredChoice();
                else options.ToolChoice = ChatToolChoice.CreateFunctionChoice(completion.ProfileOptions.ToolChoice);
            }
            // Tools and RAG DBs are not supported simultaneously, therefore RAG data is being attached at the business logic level via a direct query for now
            //if (!string.IsNullOrEmpty(completion.ProfileOptions.RagDatabase)) options = AttachDatabaseOptions(completion.ProfileOptions.RagDatabase, options);
            return options;
        }

        /// <summary>
        /// Sets the message content based on the presence of a ChatRecursion tool call.
        /// </summary>
        /// <param name="messageContent">The original message content.</param>
        /// <param name="toolCalls">The tool calls associated with the content.</param>
        /// <returns>The original messageContent, or a response to send to the next ChatRecursion LLM model.</returns>
        private string GetMessageContent(string? messageContent, Dictionary<string, string> toolCalls)
        {
            try
            {
                var content = messageContent ?? string.Empty;
                foreach (var tool in toolCalls)
                {
                    if (tool.Key.Equals(SystemTools.Chat_Recursion.ToString().ToLower())) return JsonSerializer.Deserialize<RecursiveChatSystemToolExecutionCall>(tool.Value)?.prompt_response ?? content;
                }
                return content;
            }
            catch (JsonException) { return string.Empty; }
            catch (NotSupportedException) { return string.Empty; }
            catch (ArgumentNullException) { return string.Empty; }
        }

        /// <summary>
        /// Gets the MIME type from a base64 string.
        /// </summary>
        /// <param name="base64">The string to extract the MIME type from.</param>
        /// <returns>The MIME type.</returns>
        private string GetMimeTypeFromBase64(string base64)
        {
            byte[] imageBytes = Convert.FromBase64String(base64.Substring(0, 20)); // Read only the first few bytes
            if (imageBytes.Length < 4) return "image/png";

            // Check the file signature (magic number) to determine the MIME type
            if (imageBytes.Take(4).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF })) return "image/jpeg";
            if (imageBytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 })) return "image/png";
            if (imageBytes.Take(6).SequenceEqual(new byte[] { 0x47, 0x49, 0x46, 0x38 })) return "image/gif";
            if (imageBytes.Take(4).SequenceEqual(new byte[] { 0x42, 0x4D })) return "image/bmp";
            return "image/png"; // Default if unknown
        }
    }
}
