using Azure.AI.OpenAI;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common.Extensions;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// A client for interacting with Azure OpenAI's API and services
    /// </summary>
    public class OpenAIClient : IAGIClient
    {
        private readonly string _gpt4o = "gpt-4o";
        private readonly string _gpt4oMini = "gpt-4o-mini";
        private readonly string _dalle3 = DefaultImageGenModel;
        private readonly string _dalle2 = "dall-e-2";

        private readonly ChatClient _gpt4oAIClient;
        private readonly ChatClient _gpt4ominiAIClient;
        private readonly ImageClient _qualityImageGenClient; // DALL-E 3 - does not current support image modifications and prompting is less reliable (as of 2/7/2025)
        private readonly ImageClient _versatileImageGenClient; // DALL-E 2 - currently more versatile and provides more reliable image generation via prompting (as of 2/7/2025)

        /// <summary>
        /// Creates a new instance of the OpenAIClient class
        /// </summary>
        /// <param name="settings">The AGIClient settings used to configure this client.</param>
        /// <param name="policyFactory">The client factory used to retrieve a policy.</param>
        /// <exception cref="InvalidOperationException">Thrown if the provided settings are invalid.</exception>
        public OpenAIClient(IOptionsMonitor<AGIClientSettings> settings, IHttpClientFactory policyFactory)
        {
            var policyClient = policyFactory.CreateClient(ClientPolicies.OpenAIClientPolicy.ToString());

            var service = settings.CurrentValue.OpenAIServices.Find(service => service.Endpoint == policyClient.BaseAddress?.ToString())
                ?? throw new InvalidOperationException("service key failed to be retrieved when attempting to generate a completion.");

            var credential = new ApiKeyCredential(service.Key);
            var options = new OpenAIClientOptions()
            {
                Transport = new HttpClientPipelineTransport(policyClient)
            };  
            _gpt4oAIClient = new ChatClient(_gpt4o, credential, options);
            _gpt4ominiAIClient = new ChatClient(_gpt4oMini, credential, options);

            _qualityImageGenClient = new ImageClient(_dalle3, credential, options);
            _versatileImageGenClient = new ImageClient(_dalle2, credential, options);
        }

        /// <summary>
        /// Generates an image based on the provided prompt using DALL-E 3.
        /// </summary>
        /// <param name="prompt">The prompt used to generate the image.</param>
        /// <returns>A base 64 representation of the image.</returns>
        public async Task<string?> GenerateImage(string prompt)
        {
            var options = new ImageGenerationOptions()
            {
                ResponseFormat = GeneratedImageFormat.Bytes,

                // add below to image gen system tool as arguments later potentially
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1792xH1024,
                //Style = GeneratedImageStyle.Vivid,
            };

            var completion = await _qualityImageGenClient.GenerateImageAsync(prompt, options);
            var base64Image = completion.Value.ImageBytes != null && completion.Value.ImageBytes.ToArray().Length > 0 ? Convert.ToBase64String(completion.Value.ImageBytes) : null;
            return base64Image;
        }

        /// <summary>
        /// Posts a completion request to the OpenAI client and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The CompletionRequest request details used to generate a completion.</param>
        /// <returns>The completion response.</returns>
        public async Task<CompletionResponse> PostCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);

            ChatCompletion completion;
            if (completionRequest.ProfileOptions.Model?.ToLower() == _gpt4o) completion = await _gpt4oAIClient.CompleteChatAsync(messages, options);
            else if (completionRequest.ProfileOptions.Model?.ToLower() == _gpt4oMini) completion = await _gpt4ominiAIClient.CompleteChatAsync(messages, options);
            else return new CompletionResponse() { FinishReason = FinishReason.Error };

            var content = string.Empty;
            var base64Image = string.Empty;
            foreach (var contentChunk in completion.Content)
            {
                content += contentChunk.Text ?? contentChunk.Refusal;
                if (contentChunk.ImageBytes != null && contentChunk.ImageBytes.ToArray().Length > 0) Convert.ToBase64String(contentChunk.ImageBytes);
            }

            var updatedMessages = completionRequest.Messages;
            var message = new Message()
            {
                Role = completion.Role.ToString().ConvertStringToRole(),
                Content = content,
                Base64Image = base64Image,
            };
            updatedMessages.Add(message);

            var toolCalls = new Dictionary<string, string>();
            foreach (var tool in completion.ToolCalls)
            {
                if (tool.FunctionName.ToLower() != SystemTools.Chat_Recursion.ToString().ToLower()) toolCalls.Add(tool.FunctionName, tool.FunctionArguments.ToString());
                else toolCalls.Add(SystemTools.Chat_Recursion.ToString().ToLower(), string.Empty);
            }

            return new CompletionResponse()
            {
                Messages = updatedMessages,
                ToolCalls = toolCalls,
                FinishReason = completion.FinishReason.ToString().ConvertStringToFinishReason()
            };
        }

        /// <summary>
        /// Streams a completion request to the OpenAI client and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The CompletionRequest request details used to generate a completion.</param>
        /// <returns>An asyncronous collection of CompletionStreamChunks.</returns>
        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            var options = BuildCompletionOptions(completionRequest);
            var messages = BuildCompletionMessages(completionRequest);

            AsyncCollectionResult<StreamingChatCompletionUpdate> resultCollction;
            if (completionRequest.ProfileOptions.Model?.ToLower() == _gpt4o) resultCollction = _gpt4oAIClient.CompleteChatStreamingAsync(messages);
            else if (completionRequest.ProfileOptions.Model?.ToLower() == _gpt4oMini) resultCollction = _gpt4ominiAIClient.CompleteChatStreamingAsync(messages);
            else yield break;

            var chunkId = 0;
            string role = null;
            string finishReason = null;
            var currentTool = string.Empty;
            var currentToolArgs = string.Empty;
            var toolCalls = new Dictionary<string, string>();
            await foreach (var result in resultCollction)
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

                    if(toolCalls.ContainsKey(currentTool)) toolCalls[currentTool] = currentToolArgs;
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
        /// Returns the MIME type of the image based on the first few bytes of the base64 string.
        /// </summary>
        /// <param name="base64">The base64 image to get the MIME type for.</param>
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
