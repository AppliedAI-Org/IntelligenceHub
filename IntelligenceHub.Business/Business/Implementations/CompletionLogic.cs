using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for processing requests to the completion controller.
    /// </summary>
    public class CompletionLogic : ICompletionLogic
    {
        // move to GlobalVariables class
        private const int _defaultMessageHistory = 5;
        private const int _defaultMaxRecursionMessageHistory = 20;
        private const string _defaultUser = "User";

        private readonly IAGIClientFactory _agiClientFactory;
        private readonly IAISearchServiceClient _searchClient;
        private readonly IToolClient _ToolClient;
        private readonly IProfileRepository _profileDb;
        private readonly IToolRepository _toolDb;
        private readonly IMessageHistoryRepository _messageHistoryRepository;
        private readonly IIndexMetaRepository _ragMetaRepository;

        /// <summary>
        /// A constructor utilized to resolve dependencies for the completion logic via dependency injection.
        /// </summary>
        /// <param name="agiClientFactory">Client factory used to retieve the client associated with request's host parameter.</param>
        /// <param name="searchClient">Search service client used for requests requiring RAG retrieval.</param>
        /// <param name="toolClient">HttpClient that can be used to send requests to tools that have an associated endpoint.</param>
        /// <param name="toolRepository">DAL repository to retrieve tool information.</param>
        /// <param name="profileRepository">DAL repository to retrieve profile information.</param>
        /// <param name="messageHistoryRepository">DAL repository to retrieve conversation history for previous completions.</param>
        /// <param name="indexMetaRepository">DAL repository to retrieve information about existing RAG tables.</param>
        public CompletionLogic(
            IAGIClientFactory agiClientFactory,
            IAISearchServiceClient searchClient,
            IToolClient toolClient,
            IToolRepository toolRepository,
            IProfileRepository profileRepository,
            IMessageHistoryRepository messageHistoryRepository,
            IIndexMetaRepository indexMetaRepository)
        {
            _toolDb = toolRepository;
            _ToolClient = toolClient;
            _profileDb = profileRepository;
            _messageHistoryRepository = messageHistoryRepository;
            _ragMetaRepository = indexMetaRepository;
            _searchClient = searchClient;
            _agiClientFactory = agiClientFactory;
        }

        #region Streaming

        /// <summary>
        /// Streams completion updates to the requesting client.
        /// </summary>
        /// <param name="completionRequest">The body of the completion request.</param>
        /// <returns>An asyncronous enumerable that contain the completion response generated from an AGI client.</returns>
        public async IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest)
        {
            if (completionRequest.ProfileOptions == null || string.IsNullOrEmpty(completionRequest.ProfileOptions.Name)) yield break;

            var profile = await _profileDb.GetByNameAsync(completionRequest.ProfileOptions.Name);
            var mappedProfile = DbMappingHandler.MapFromDbProfile(profile);
            completionRequest.ProfileOptions = await BuildCompletionOptions(mappedProfile, completionRequest.ProfileOptions);

            // Get and attach message history if a conversation id exists
            if (completionRequest.ConversationId is Guid conversationId)
            {
                completionRequest.Messages = await BuildMessageHistory(
                    conversationId,
                    completionRequest.Messages,
                    completionRequest.ProfileOptions.MaxMessageHistory);
            }

            // construct AGI Client based on the required host
            if (completionRequest.ProfileOptions.Host == null) yield break;
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);

            // Add data retrieved from RAG indexing
            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase))
            {
                var completionMessage = completionRequest.Messages.LastOrDefault();
                if (completionMessage == null) yield break;

                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionRequest, agiClient);
                if (completionMessageWithRagData != null)
                {
                    completionRequest.Messages.Remove(completionMessage);
                    completionRequest.Messages.Add(completionMessageWithRagData);
                }
            }

            var completionCollection = agiClient.StreamCompletion(completionRequest);
            if (completionCollection == null) yield break;

            Role? dbMessageRole;
            var allCompletionChunks = string.Empty;
            var toolCallDictionary = new Dictionary<string, string>();
            await foreach (var update in completionCollection)
            {
                if (update == null) continue;

                toolCallDictionary = update.ToolCalls;
                allCompletionChunks += update.CompletionUpdate;

                var roleString = update.Role.ToString();
                if (!string.IsNullOrEmpty(roleString))
                {
                    dbMessageRole = roleString.ConvertStringToRole();
                    update.Role = dbMessageRole;
                }
                yield return update;
            }

            if (toolCallDictionary.Any())
            {
                if (!string.IsNullOrEmpty(allCompletionChunks)) completionRequest.Messages.Add(new Message() { Content = allCompletionChunks, Role = Role.Assistant, User = _defaultUser, TimeStamp = DateTime.UtcNow });
                var (toolExecutionResponses, recursiveMessages) = await ExecuteTools(toolCallDictionary, completionRequest.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId);

                var completionString = string.Empty;
                if (recursiveMessages.Any()) foreach (var message in recursiveMessages) completionString += $"{message.User}: {message.Content}\n;";
                if (toolExecutionResponses.Any()) yield return new CompletionStreamChunk()
                {
                    CompletionUpdate = completionString,
                    ToolCalls = toolCallDictionary,
                    ToolExecutionResponses = toolExecutionResponses,
                    FinishReason = FinishReason.ToolCalls,
                };
            }

            // save new messages to database
            if (completionRequest.ConversationId is Guid id)
            {
                var lastUserMessage = completionRequest.Messages.Last(m => m.Role == Role.User);
                var dbUserMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.User, Content = lastUserMessage.Content, TimeStamp = lastUserMessage.TimeStamp, User = _defaultUser }, id);
                await _messageHistoryRepository.AddAsync(dbUserMessage);

                var dbMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.Assistant, Content = allCompletionChunks, TimeStamp = DateTime.UtcNow, User = completionRequest.ProfileOptions.Name ?? string.Empty }, id);
                await _messageHistoryRepository.AddAsync(dbMessage);
            }
        }

        #endregion

        #region Controller

        /// <summary>
        /// Processes a completion request and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The body of the completion request.</param>
        /// <returns>A completion generated by an AGI client.</returns>
        public async Task<CompletionResponse?> ProcessCompletion(CompletionRequest completionRequest)
        {
            if (!completionRequest.Messages.Any() || string.IsNullOrEmpty(completionRequest.ProfileOptions.Name)) return null;

            var profile = await _profileDb.GetByNameAsync(completionRequest.ProfileOptions.Name);
            if (profile == null) return null;

            var mappedProfile = DbMappingHandler.MapFromDbProfile(profile);
            completionRequest.ProfileOptions = await BuildCompletionOptions(mappedProfile, completionRequest.ProfileOptions);

            if (completionRequest.ConversationId is Guid conversationId)
            {
                completionRequest.Messages = await BuildMessageHistory(
                    conversationId,
                    completionRequest.Messages,
                    completionRequest.ProfileOptions.MaxMessageHistory);
            }

            // construct AGI Client based on the required host
            if (completionRequest.ProfileOptions.Host == null) return null;
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);

            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase))
            {
                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionRequest, agiClient);
                if (completionMessageWithRagData != null)
                {
                    var completionMessage = completionRequest.Messages.LastOrDefault();
                    completionRequest.Messages.Remove(completionMessage);
                    completionRequest.Messages.Add(completionMessageWithRagData);
                }
            }

            var completion = await agiClient.PostCompletion(completionRequest);
            if (completion.FinishReason == FinishReason.Error) return completion;

            if (completionRequest.ConversationId is Guid id)
            {
                var compUserMessage = completion.Messages.Last(m => m.Role == Role.User);
                compUserMessage.User = _defaultUser;
                var dbUserMessage = DbMappingHandler.MapToDbMessage(compUserMessage, id);
                await _messageHistoryRepository.AddAsync(dbUserMessage);

                var compMessage = completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool);
                compMessage.User = completionRequest.ProfileOptions.Name;
                var dbMessage = DbMappingHandler.MapToDbMessage(compMessage, id);
                await _messageHistoryRepository.AddAsync(dbMessage);
            }

            if (completion.FinishReason == FinishReason.ToolCalls)
            {
                var (toolExecutionResponses, recursiveMessages) = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId);
                if (toolExecutionResponses.Any()) completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
                if (recursiveMessages.Any()) completion.Messages = recursiveMessages;
            }
            return completion;
        }

        /// <summary>
        /// Retrieves and builds the message history for a conversation.
        /// </summary>
        /// <param name="conversationId">The Id of the conversation being retrieved from the database.</param>
        /// <param name="requestMessages">The list of messages associated with the current completion request.</param>
        /// <param name="maxMessageHistory">The maximum amount of messages to include in the list of messages.</param>
        /// <returns>A list of messages associated with the conversationId.</returns>
        private async Task<List<Message>> BuildMessageHistory(Guid conversationId, List<Message> requestMessages, int? maxMessageHistory = null)
        {
            var allMessages = new List<Message>();
            var messageHistory = await _messageHistoryRepository.GetConversationAsync(conversationId, maxMessageHistory ?? _defaultMessageHistory, 1);
            if (messageHistory == null || messageHistory.Count < 1) return requestMessages; // no conversation found, return original data and create conversation entry later

            var mappedMessageHistory = new List<Message>();
            foreach (var message in messageHistory) mappedMessageHistory.Add(DbMappingHandler.MapFromDbMessage(message));

            // ensure the messages are properly arranged, with the user completion appearing very last
            allMessages.AddRange(mappedMessageHistory);
            allMessages.AddRange(requestMessages);

            return allMessages;
        }

        /// <summary>
        /// Builds the completion options utilizing an existing profile, and overriding these options
        /// in the request body.
        /// </summary>
        /// <param name="profile">The profile as stored in the database.</param>
        /// <param name="profileOptions">The profile options in the request body that can be used to override existing settings.</param>
        /// <returns>The profile updated with any existing, or overridden values.</returns>
        public async Task<Profile> BuildCompletionOptions(Profile profile, Profile? profileOptions = null)
        {
            // adds the profile references to the tools
            var profileReferences = profileOptions?.ReferenceProfiles ?? profile.ReferenceProfiles ?? Array.Empty<string>();
            if (profile.Tools == null) profile.Tools = new List<Tool>();

            // add image gen system tool if appropriate - Anthropic does not support image gen currently
            var host = profileOptions?.ImageHost ?? profile.ImageHost ?? profileOptions?.Host ?? profile.Host; // if an image host is provided use that, otherwise default to the primary host
            if (host != AGIServiceHosts.Anthropic && host != AGIServiceHosts.None) profile.Tools.Add(new ImageGenSystemTool());

            // add recursive chat system tool if appropriate
            if (profileReferences != null && profileReferences.Any())
            {
                // add to both tool sets to ensure no overwrites occur
                var recursionTool = await BuildProfileReferenceTool(profileReferences);
                profileOptions?.Tools?.Add(recursionTool);
                profile.Tools.Add(recursionTool);
            }

            // overwrite defaults if ProfileOptions contains values
            return new Profile()
            {
                Id = profile.Id,
                Name = profile.Name,
                Model = profileOptions?.Model ?? profile.Model,
                RagDatabase = profileOptions?.RagDatabase ?? profile.RagDatabase,
                MaxMessageHistory = profileOptions?.MaxMessageHistory ?? profile.MaxMessageHistory,
                MaxTokens = profileOptions?.MaxTokens ?? profile.MaxTokens,
                Temperature = profileOptions?.Temperature ?? profile.Temperature,
                TopP = profileOptions?.TopP ?? profile.TopP,
                FrequencyPenalty = profileOptions?.FrequencyPenalty ?? profile.FrequencyPenalty,
                PresencePenalty = profileOptions?.PresencePenalty ?? profile.PresencePenalty,
                Stop = profileOptions?.Stop ?? profile.Stop,
                Logprobs = profileOptions?.Logprobs ?? profile.Logprobs,
                TopLogprobs = profileOptions?.TopLogprobs ?? profile.TopLogprobs,
                ResponseFormat = profileOptions?.ResponseFormat ?? profile.ResponseFormat,
                User = profileOptions?.User ?? profile.User,
                Tools = profileOptions?.Tools ?? profile.Tools,
                SystemMessage = profileOptions?.SystemMessage ?? profile.SystemMessage,
                ReferenceProfiles = profileReferences,
                Host = profileOptions?.Host ?? profile.Host,
                ToolChoice = profileOptions?.ToolChoice ?? profile.ToolChoice,
                ReferenceDescription = profileOptions?.ReferenceDescription ?? profile.ReferenceDescription
            };
        }
        #endregion

        #region Shared

        /// <summary>
        /// Builds a recursive chat system tool based on the profile references parameter.
        /// </summary>
        /// <param name="profileNames">The profiles used to create a recursive chat tool.</param>
        /// <returns>A Recursive Chat System tool that can be called by AGI client models.</returns>
        private async Task<RecursiveChatSystemTool> BuildProfileReferenceTool(string[] profileNames)
        {
            var referenceProfiles = new List<Profile>();
            foreach (var name in profileNames)
            {
                var profile = await _profileDb.GetByNameAsync(name);
                if (profile == null) continue;
                referenceProfiles.Add(DbMappingHandler.MapFromDbProfile(profile));
            }
            return new RecursiveChatSystemTool(referenceProfiles);
        }

        /// <summary>
        /// Processes and executes any tool calls that are present in a completion response.
        /// </summary>
        /// <param name="toolCalls">A dictionary of function names, and their arguemnts.</param>
        /// <param name="messages">The conversation history used as context for the Chat Recursion system tool.</param>
        /// <param name="options">The AI client profile options associated with the request being processed.</param>
        /// <param name="conversationId">The Id of the conversation being processed.</param>
        /// <param name="streaming">Boolean to indicate the request is streamed.</param>
        /// <param name="currentRecursionDepth">The current depth of recursion used to prevent infinite looping
        /// resulting from the Chat Recursion system tool.</param>
        /// <returns>A tuple of the http responses assiciated with tools executed by the tool client, and any 
        /// new messages generated from the Chat Recursion system tool.</returns>
        public async Task<(List<HttpResponseMessage>, List<Message>)> ExecuteTools(Dictionary<string, string> toolCalls, List<Message> messages, Profile? options = null, Guid? conversationId = null, int currentRecursionDepth = 0)
        {
            var functionResults = new List<HttpResponseMessage>();
            var maxDepth = options?.MaxMessageHistory ?? _defaultMaxRecursionMessageHistory;
            foreach (var tool in toolCalls)
            {
                if (tool.Key.ToLower().Equals(SystemTools.Chat_Recursion.ToString().ToLower()) || currentRecursionDepth > maxDepth) messages = await HandleRecursiveChat(tool.Value, messages, options, conversationId, currentRecursionDepth + 1);
                else if (tool.Key.ToLower().Equals(SystemTools.Image_Gen.ToString().ToLower()) || currentRecursionDepth > maxDepth) messages = await GenerateImage(tool.Value, options?.ImageHost ?? options?.Host, messages);
                else
                {
                    var dbTool = await _toolDb.GetByNameAsync(tool.Key);
                    if (dbTool != null && !string.IsNullOrEmpty(dbTool.ExecutionUrl)) functionResults.Add(await _ToolClient.CallFunction(tool.Key, tool.Value, dbTool.ExecutionUrl, dbTool.ExecutionMethod, dbTool.ExecutionBase64Key));
                }
            }
            return (functionResults, messages);
        }

        /// <summary>
        /// Handles the recursive chat system tool by calling the appropriate AI model
        /// and managing completion context.
        /// </summary>
        /// <param name="toolCall">The arguments passed to the Recursive Chat tool.</param>
        /// <param name="messages">A list of messages representing the conversation context.</param>
        /// <param name="options">The options associated with the current profile to generate the next completion.</param>
        /// <param name="conversationId">The Id of the conversation if the conversation is being saved.</param>
        /// <param name="currentRecursionDepth">The current depth of recursion used to prevent infinite looping
        /// resulting from the Chat Recursion system tool.</param>
        /// <returns>An updated list of messages representing the conversation context.</returns>
        private async Task<List<Message>> HandleRecursiveChat(string toolCall, List<Message> messages, Profile? options = null, Guid? conversationId = null, int currentRecursionDepth = 0)
        {
            var toolExecutionCall = JsonSerializer.Deserialize<RecursiveChatSystemToolExecutionCall>(toolCall);
            if (toolExecutionCall == null) return messages;

            var profileName = toolExecutionCall.responding_ai_model;
            if (string.IsNullOrEmpty(profileName)) return messages;

            var recursionProfile = await _profileDb.GetByNameAsync(profileName);
            if (recursionProfile == null) return messages;

            var completionRequest = new CompletionRequest()
            {
                ConversationId = conversationId,
                Messages = messages,
                ProfileOptions = DbMappingHandler.MapFromDbProfile(recursionProfile)
            };

            var maxMessageHistory = completionRequest.ProfileOptions.MaxMessageHistory ?? _defaultMaxRecursionMessageHistory;
            if (currentRecursionDepth < maxMessageHistory)
            {
                var recursionCompletion = await ProcessRecursiveCompletion(completionRequest, currentRecursionDepth);
                if (recursionCompletion != null) messages = recursionCompletion.Messages;
            }
            return messages;
        }

        /// <summary>
        /// Processes a recursive completion request and returns the completion response.
        /// </summary>
        /// <param name="completionRequest">The request body for the request.</param>
        /// <param name="currentRecursionDepth">The current depth of recursion used to prevent infinite loops.</param>
        /// <returns>A completion containing the AGI client's response.</returns>
        private async Task<CompletionResponse?> ProcessRecursiveCompletion(CompletionRequest completionRequest, int currentRecursionDepth)
        {
            if (!completionRequest.Messages.Any() || string.IsNullOrEmpty(completionRequest.ProfileOptions.Name)) return null;

            // reorganize and combine messages of same role in order to reduce model performance deprecation
            var MessagesAfterRoleDistribution = new List<Message>();
            var messages = completionRequest.Messages;

            // copy the list, creating a seperate ref
            var originalMessageDistribution = new List<Message>(completionRequest.Messages.Select(m => new Message 
            {
                Role = m.Role,
                User = m.User,
                Content = m.Content,
                Base64Image = m.Base64Image,
                TimeStamp = m.TimeStamp
            }));
            
            for (var i = 0; i < messages.Count; i++)
            {
                // Determine the role of the current message
                if (i == messages.Count - 1) messages[i].Role = Role.User; // Set the last message as User
                else if (messages[i].User.ToLower() == completionRequest.ProfileOptions.Name.ToLower()) messages[i].Role = Role.Assistant; // Set as Assistant
                else messages[i].Role = Role.User; // Set as User

                // Combine consecutive messages with the same role or add the message to the list if the role is different
                if (MessagesAfterRoleDistribution.Count > 0 && MessagesAfterRoleDistribution[^1].Role == messages[i].Role) MessagesAfterRoleDistribution[^1].Content += "\n\n" + messages[i].Content;
                else MessagesAfterRoleDistribution.Add(messages[i]);
            }

            completionRequest.Messages = MessagesAfterRoleDistribution;

            // Build Options
            completionRequest.ProfileOptions = await BuildCompletionOptions(completionRequest.ProfileOptions);

            // construct AGI Client based on the required host
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);

            // Add data retrieved from RAG indexing
            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase) && completionRequest.Messages.Any())
            {
                var completionMessage = completionRequest.Messages.LastOrDefault() ?? new Message() { Role = Role.User, User = _defaultUser, Content = string.Empty, TimeStamp = DateTime.UtcNow };
                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionRequest, agiClient);
                if (completionMessageWithRagData != null)
                {
                    completionRequest.Messages.Remove(completionMessage);
                    completionRequest.Messages.Add(completionMessageWithRagData);
                }
            }

            var completion = await agiClient.PostCompletion(completionRequest);
            if (completion.FinishReason == FinishReason.Error) return completion;

            if (completionRequest.ConversationId is Guid id)
            {
                var dbMessage = DbMappingHandler.MapToDbMessage(completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool), id);
                dbMessage.User = completionRequest.ProfileOptions.Name ?? string.Empty;
                await _messageHistoryRepository.AddAsync(dbMessage);
            }

            // restore original message distribution with the latest data appended
            originalMessageDistribution.Add(completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool));
            completion.Messages = originalMessageDistribution;
            completion.Messages.Last().User = completionRequest.ProfileOptions.Name;
            
            if (completion.FinishReason == FinishReason.ToolCalls)
            {
                var (toolExecutionResponses, recursiveMessages) = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, currentRecursionDepth: currentRecursionDepth);
                if (toolExecutionResponses.Any()) completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
                if (recursiveMessages.Any()) completion.Messages = recursiveMessages;
            }
            return completion;
        }

        /// <summary>
        /// Retrieves RAG data from the specified index and appends it to the last message in the completion request.
        /// </summary>
        /// <param name="indexName">The name of the RAG index to search.</param>
        /// <param name="originalRequest">The original completion request used to generate a search intent.</param>
        /// <param name="agiClient">The AGI client that will be used to generate a search intent.</param>
        /// <returns>The original message with any relevant RAG documents attached in the content.</returns>
        private async Task<Message?> RetrieveRagData(string indexName, CompletionRequest originalRequest, IAGIClient agiClient)
        {
            var dbIndex = await _ragMetaRepository.GetByNameAsync(indexName);
            if (!originalRequest.Messages.Any() || dbIndex == null) return null;

            var originalMessageContent = originalRequest.Messages.LastOrDefault()?.Content ?? string.Empty;

            // Create a copy of the original request with modified content (without altering the original)
            var modifiedRequest = new CompletionRequest
            {
                // Only copy in relevant data to prevent unnescesary tool calls utilization, and similar issues
                ProfileOptions = new Profile() { Name = originalRequest.ProfileOptions.Name, Model = originalRequest.ProfileOptions.Model }, 
                Messages = new List<Message>(originalRequest.Messages)
            };

            var finalMessage = modifiedRequest.Messages.LastOrDefault() ?? new Message() { Role = Role.User, Content = "Please search the database for any information", TimeStamp = DateTime.UtcNow };
            finalMessage.Content = "Please use the below data to construct an intentful natural language query that can be used to search a RAG database. " +
                                   "If the message does not seem to require a query to be constructed, please respond with a single .\n\n" + originalMessageContent;

            var completionResponse = await agiClient.PostCompletion(modifiedRequest);
            var intentfulQuery = completionResponse.Messages.LastOrDefault()?.Content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(intentfulQuery)) return null;

            var indexData = DbMappingHandler.MapFromDbIndexMetadata(dbIndex);
            var ragData = await _searchClient.SearchIndex(indexData, intentfulQuery);

            var resultCollection = ragData.GetResultsAsync();
            var ragDataString = string.Empty;

            await foreach (var item in resultCollection)
            {
                if (indexData.QueryType == QueryType.Semantic)
                {
                    var semanticResult = item.SemanticSearch;
                    ragDataString += $"\n```" +
                                     $"\nTitle: {item.Document.Title}" +
                                     $"\nSource: {item.Document.Source}" +
                                     $"\nCreation Date: {item.Document.Created:yyyy-MM-ddTHH:mm:ss}" +
                                     $"\nLast Updated Date: {item.Document.Modified:yyyy-MM-ddTHH:mm:ss}";
                    foreach (var caption in semanticResult.Captions) ragDataString += $"\nContent Chunk: {caption.Text}";
                    ragDataString += $"\n```\n";
                }
                else
                {
                    ragDataString += $"\n```" +
                                     $"\nTitle: {item.Document.Title}" +
                                     $"\nSource: {item.Document.Source}" +
                                     $"\nCreation Date: {item.Document.Created:yyyy-MM-ddTHH:mm:ss}" +
                                     $"\nLast Updated Date: {item.Document.Modified:yyyy-MM-ddTHH:mm:ss}" +
                                     $"\nContent: {item.Document.chunk}" +
                                     $"\n```\n";
                }
            }

            // Construct a new message with the appended RAG data
            var messageWithRagAppended = new Message
            {
                Role = finalMessage.Role,
                User = finalMessage.User,
                Base64Image = finalMessage.Base64Image,
                TimeStamp = finalMessage.TimeStamp,
                Content = $"\n\nBelow is a list of documents, each of which is delimited with triple backticks. If relevant, " +
                          $"and a source is present, please cite these documents in markdown plain in-text citation when " +
                          $"responding to the following prompt: {originalMessageContent}\n\n" + ragDataString
            };
            return messageWithRagAppended;
        }

        /// <summary>
        /// Generates an image based on the prompt provided in the completion request.
        /// </summary>
        /// <param name="imageGenArgs">The arguments assocaited with an image generation tool call.</param>
        /// <param name="host">The name of the AGI client that will be used to generate the image.</param>
        /// <param name="messages">A list of messages representing the current conversation context.</param>
        /// <returns>A list of messages representing the current conversation context with the generated 
        /// image attached.</returns>
        private async Task<List<Message>> GenerateImage(string imageGenArgs, AGIServiceHosts? host, List<Message> messages)
        {
            // anthropic does not support image gen currently
            if (host == AGIServiceHosts.Anthropic) return messages;

            var imageGenArgsJson = JsonSerializer.Deserialize<Dictionary<string, string>>(imageGenArgs);
            if (imageGenArgsJson == null || !imageGenArgsJson.ContainsKey("prompt")) return messages;

            var prompt = imageGenArgsJson["prompt"];

            var client = _agiClientFactory.GetClient(host);
            var base64Image = await client.GenerateImage(prompt);

            // Append the generated image to the last message
            var lastMessage = messages.LastOrDefault();
            if (lastMessage != null && !string.IsNullOrEmpty(base64Image)) lastMessage.Base64Image = base64Image ?? string.Empty;
            else if (!string.IsNullOrEmpty(base64Image)) messages.Add(new Message() { Content = string.Empty, Role = Role.Assistant, TimeStamp = DateTime.UtcNow, Base64Image = base64Image ?? string.Empty });
            return messages;
        }
#endregion
    }
}
