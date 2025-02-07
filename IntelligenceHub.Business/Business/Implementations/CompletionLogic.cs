using Azure.Search.Documents.Models;
using IntelligenceHub.API.API.DTOs.Tools;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
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

        public CompletionLogic(
            IAGIClientFactory agiClientFactory,
            IAISearchServiceClient searchClient,
            IToolClient ToolClient,
            IToolRepository toolRepository,
            IProfileRepository profileRepository,
            IMessageHistoryRepository messageHistoryRepository,
            IIndexMetaRepository indexMetaRepository)
        {
            _toolDb = toolRepository;
            _ToolClient = ToolClient;
            _profileDb = profileRepository;
            _messageHistoryRepository = messageHistoryRepository;
            _ragMetaRepository = indexMetaRepository;
            _searchClient = searchClient;
            _agiClientFactory = agiClientFactory;
        }

        #region Streaming

        // Create a method for the parts of this that are identical to those in ProcessCompletion
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

            // Add data retrieved from RAG indexing
            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase))
            {
                var completionMessage = completionRequest.Messages.LastOrDefault();
                if (completionMessage == null) yield break;

                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionMessage);
                completionRequest.Messages.Remove(completionMessage);
                completionRequest.Messages.Add(completionMessageWithRagData);
            }

            if (completionRequest.ProfileOptions.Host == null) yield break;
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);
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
                var (toolExecutionResponses, recursiveMessages) = await ExecuteTools(toolCallDictionary, completionRequest.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, streaming: true);

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

        public async Task<CompletionResponse?> ProcessCompletion(CompletionRequest completionRequest)
        {
            if (!completionRequest.Messages.Any() || string.IsNullOrEmpty(completionRequest.ProfileOptions.Name)) return null;

            var profile = await _profileDb.GetByNameAsync(completionRequest.ProfileOptions.Name);
            var mappedProfile = DbMappingHandler.MapFromDbProfile(profile);
            completionRequest.ProfileOptions = await BuildCompletionOptions(mappedProfile, completionRequest.ProfileOptions);

            if (completionRequest.ConversationId is Guid conversationId)
            {
                completionRequest.Messages = await BuildMessageHistory(
                    conversationId,
                    completionRequest.Messages,
                    completionRequest.ProfileOptions.MaxMessageHistory);
            }

            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase))
            {
                var completionMessage = completionRequest.Messages.LastOrDefault();
                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionMessage);
                completionRequest.Messages.Remove(completionMessage);
                completionRequest.Messages.Add(completionMessageWithRagData);
            }

            if (completionRequest.ProfileOptions.Host == null) return null;
            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);
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
                var (toolExecutionResponses, recursiveMessages) = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, streaming: false);
                if (toolExecutionResponses.Any()) completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
                if (recursiveMessages.Any()) completion.Messages = recursiveMessages;
            }
            return completion;
        }

        private async Task<List<Message>> BuildMessageHistory(Guid conversationId, List<Message> requestMessages, int? maxMessageHistory = null)
        {
            var allMessages = new List<Message>();
            var messageHistory = await _messageHistoryRepository.GetConversationAsync(conversationId, maxMessageHistory ?? _defaultMessageHistory);
            if (messageHistory == null || messageHistory.Count < 1) return requestMessages; // no conversation found, return original data and create conversation entry later

            var mappedMessageHistory = new List<Message>();
            foreach (var message in messageHistory) mappedMessageHistory.Add(DbMappingHandler.MapFromDbMessage(message));

            // ensure the messages are properly arranged, with the user completion appearing very last
            allMessages.AddRange(mappedMessageHistory);
            allMessages.AddRange(requestMessages);

            return allMessages;
        }

        public async Task<Profile> BuildCompletionOptions(Profile profile, Profile? profileOptions = null)
        {
            // adds the profile references to the tools
            var profileReferences = profileOptions?.Reference_Profiles ?? profile.Reference_Profiles ?? Array.Empty<string>();
            if (profile.Tools == null) profile.Tools = new List<Tool>();
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
                Max_Tokens = profileOptions?.Max_Tokens ?? profile.Max_Tokens,
                Temperature = profileOptions?.Temperature ?? profile.Temperature,
                Top_P = profileOptions?.Top_P ?? profile.Top_P,
                Frequency_Penalty = profileOptions?.Frequency_Penalty ?? profile.Frequency_Penalty,
                Presence_Penalty = profileOptions?.Presence_Penalty ?? profile.Presence_Penalty,
                Stop = profileOptions?.Stop ?? profile.Stop,
                Logprobs = profileOptions?.Logprobs ?? profile.Logprobs,
                Top_Logprobs = profileOptions?.Top_Logprobs ?? profile.Top_Logprobs,
                Response_Format = profileOptions?.Response_Format ?? profile.Response_Format,
                User = profileOptions?.User ?? profile.User,
                Tools = profileOptions?.Tools ?? profile.Tools,
                System_Message = profileOptions?.System_Message ?? profile.System_Message,
                Reference_Profiles = profileReferences,
                Host = profileOptions?.Host ?? profile.Host,
                Tool_Choice = profileOptions?.Tool_Choice ?? profile.Tool_Choice,
                ReferenceDescription = profileOptions?.ReferenceDescription ?? profile.ReferenceDescription
            };
        }
        #endregion

        #region Shared

        private async Task<ProfileReferenceTool> BuildProfileReferenceTool(string[] profileNames)
        {
            var referenceProfiles = new List<Profile>();
            foreach (var name in profileNames)
            {
                var profile = await _profileDb.GetByNameAsync(name);
                if (profile == null) continue;
                referenceProfiles.Add(DbMappingHandler.MapFromDbProfile(profile));
            }
            return new ProfileReferenceTool(referenceProfiles);
        }

        public async Task<(List<HttpResponseMessage>, List<Message>)> ExecuteTools(Dictionary<string, string> toolCalls, List<Message> messages, Profile? options = null, Guid? conversationId = null, bool streaming = false, int currentRecursionDepth = 0)
        {
            var functionResults = new List<HttpResponseMessage>();
            var maxDepth = options?.MaxMessageHistory ?? _defaultMaxRecursionMessageHistory;
            foreach (var tool in toolCalls)
            {
                if (tool.Key.Equals(SystemTools.Recurse_ai_dialogue.ToString().ToLower()) || currentRecursionDepth > maxDepth) messages = await HandleRecursiveDialogue(tool.Value, messages, options, conversationId, currentRecursionDepth + 1);
                else
                {
                    var dbTool = await _toolDb.GetByNameAsync(tool.Key);
                    if (dbTool != null && !string.IsNullOrEmpty(dbTool.ExecutionUrl)) functionResults.Add(await _ToolClient.CallFunction(tool.Key, tool.Value, dbTool.ExecutionUrl, dbTool.ExecutionMethod, dbTool.ExecutionBase64Key));
                }
            }
            return (functionResults, messages);
        }

        private async Task<List<Message>> HandleRecursiveDialogue(string toolCall, List<Message> messages, Profile? options = null, Guid? conversationId = null, int currentRecursionDepth = 0)
        {
            var toolExecutionCall = JsonSerializer.Deserialize<ProfileReferenceToolExecutionCall>(toolCall);
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

            // Add data retrieved from RAG indexing
            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase) && completionRequest.Messages.Any())
            {
                var completionMessage = completionRequest.Messages.LastOrDefault() ?? new Message() { Role = Role.User, User = _defaultUser, Content = string.Empty, TimeStamp = DateTime.UtcNow };
                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionMessage);
                completionRequest.Messages.Remove(completionMessage);
                completionRequest.Messages.Add(completionMessageWithRagData);
            }

            var agiClient = _agiClientFactory.GetClient(completionRequest.ProfileOptions.Host);
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
                var (toolExecutionResponses, recursiveMessages) = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, streaming: false, currentRecursionDepth: currentRecursionDepth);
                if (toolExecutionResponses.Any()) completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
                if (recursiveMessages.Any()) completion.Messages = recursiveMessages;
            }
            return completion;
        }

        private async Task<Message> RetrieveRagData(string indexName, Message completion)
        {
            var dbIndex = await _ragMetaRepository.GetByNameAsync(indexName);

            var indexData = DbMappingHandler.MapFromDbIndexMetadata(dbIndex);
            var ragData = await _searchClient.SearchIndex(indexData, completion.Content);

            var resultCollection = ragData.GetResultsAsync();
            var ragDataString = string.Empty;
            await foreach (var item in resultCollection)
            {
                if (indexData.QueryType == SearchQueryType.Semantic.ToString())
                {
                    var semanticResult = item.SemanticSearch;
                    ragDataString += $"\n```\n" +
                                     $"\nTitle: {item.Document.title}" +
                                     $"\nSource: {item.Document.source}" +
                                     $"\nCreation Date: {item.Document.created.ToString("yyyy-MM-ddTHH:mm:ss")}" +
                                     $"\nLast Updated Date: {item.Document.modified.ToString("yyyy-MM-ddTHH:mm:ss")}";
                    foreach (var caption in semanticResult.Captions) ragDataString += $"\nContent Chunk: {caption.Text}";
                    ragDataString += $"\n```\n";
                }
                else
                {
                    ragDataString += $"\n```\n" +
                                     $"\nTitle: {item.Document.title}" +
                                     $"\nSource: {item.Document.source}" +
                                     $"\nCreation Date: {item.Document.created.ToString("yyyy-MM-ddTHH:mm:ss")}" +
                                     $"\nLast Updated Date: {item.Document.modified.ToString("yyyy-MM-ddTHH:mm:ss")}" +
                                     $"\nContent: {item.Document.content}" +
                                     $"\n```\n";
                }
            }

            completion.Content = $"\n\nBelow is a list of documents, each of which is delimited with triple backticks. If relevant, " +
                $"please cite these documents in markdown plain in-text citation (i.e. '(Title)[Source]') when responding to the " +
                $"following prompt: {completion.Content}\n\n"
                + ragDataString;

            return completion;
        }
#endregion
    }
}
