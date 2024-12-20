using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    public class CompletionLogic : ICompletionLogic
    {
        // move to GlobalVariables class
        private const int _defaultMessageHistory = 5;

        private readonly IAGIClient _AIClient;
        private readonly IAISearchServiceClient _searchClient;
        private readonly IToolClient _ToolClient;
        private readonly IProfileRepository _profileDb;
        private readonly IToolRepository _toolDb;
        private readonly IMessageHistoryRepository _messageHistoryRepository;
        private readonly IIndexMetaRepository _ragMetaRepository;

        public CompletionLogic(
            IAGIClient agiClient,
            IAISearchServiceClient searchClient,
            IToolClient ToolClient,
            IToolRepository toolRepository,
            IProfileRepository profileRepository,
            IMessageHistoryRepository messageHistoryRepository,
            IIndexMetaRepository indexMetaRepository)
        {
            _toolDb = toolRepository;
            _AIClient = agiClient;
            _ToolClient = ToolClient;
            _profileDb = profileRepository;
            _messageHistoryRepository = messageHistoryRepository;
            _ragMetaRepository = indexMetaRepository;
            _searchClient = searchClient;
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
                completionRequest.Messages = await BuildAndUpdateMessageHistory(
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

            var completionCollection = _AIClient.StreamCompletion(completionRequest);
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
                if (!string.IsNullOrEmpty(allCompletionChunks)) completionRequest.Messages.Add(new Message() { Content = allCompletionChunks, Role = Role.Assistant, TimeStamp = DateTime.UtcNow });
                var toolExecutionResponses = await ExecuteTools(toolCallDictionary, completionRequest.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, streaming: true);
                yield return new CompletionStreamChunk()
                {
                    ToolCalls = toolCallDictionary,
                    ToolExecutionResponses = toolExecutionResponses,
                    FinishReason = FinishReason.ToolCalls,
                };
            }

            // save new messages to database
            if (completionRequest.ConversationId is Guid id)
            {
                var lastUserMessage = completionRequest.Messages.Last(m => m.Role == Role.User);
                var dbUserMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.User, Content = lastUserMessage.Content, TimeStamp = lastUserMessage.TimeStamp }, id, null);
                await _messageHistoryRepository.AddAsync(dbUserMessage);

                var dbMessage = DbMappingHandler.MapToDbMessage(new Message() { Role = Role.Assistant, Content = allCompletionChunks, TimeStamp = DateTime.UtcNow }, id, toolCallDictionary.Keys.ToArray());
                await _messageHistoryRepository.AddAsync(dbMessage);
            }
        }

        #endregion

        #region Controller

        public async Task<CompletionResponse?> ProcessCompletion(CompletionRequest completionRequest)
        {
            if (!completionRequest.Messages.Any() || string.IsNullOrEmpty(completionRequest.ProfileOptions.Name)) return null;

            // Get and set profile details, overriding the database with any parameters that aren't null in the request
            var profile = await _profileDb.GetByNameAsync(completionRequest.ProfileOptions.Name);
            var mappedProfile = DbMappingHandler.MapFromDbProfile(profile);
            completionRequest.ProfileOptions = await BuildCompletionOptions(mappedProfile, completionRequest.ProfileOptions);

            // Get and attach message history if a conversation id exists
            if (completionRequest.ConversationId is Guid conversationId)
            {
                completionRequest.Messages = await BuildAndUpdateMessageHistory(
                    conversationId,
                    completionRequest.Messages,
                    completionRequest.ProfileOptions.MaxMessageHistory);
            }

            // Add data retrieved from RAG indexing
            if (!string.IsNullOrEmpty(completionRequest.ProfileOptions.RagDatabase))
            {
                var completionMessage = completionRequest.Messages.LastOrDefault();
                var completionMessageWithRagData = await RetrieveRagData(completionRequest.ProfileOptions.RagDatabase, completionMessage);
                completionRequest.Messages.Remove(completionMessage);
                completionRequest.Messages.Add(completionMessageWithRagData);
            }


            var completion = await _AIClient.PostCompletion(completionRequest);
            if (completion.FinishReason == FinishReason.Error) return completion;

            if (completionRequest.ConversationId is Guid id)
            {
                var dbUserMessage = DbMappingHandler.MapToDbMessage(completion.Messages.Last(m => m.Role == Role.User), id, null);
                await _messageHistoryRepository.AddAsync(dbUserMessage);

                var dbMessage = DbMappingHandler.MapToDbMessage(completion.Messages.Last(m => m.Role == Role.Assistant || m.Role == Role.Tool), id, completion.ToolCalls.Keys.ToArray());
                await _messageHistoryRepository.AddAsync(dbMessage);
            }

            if (completion.FinishReason == FinishReason.ToolCalls)
            {
                var toolExecutionResponses = await ExecuteTools(completion.ToolCalls, completion.Messages, completionRequest.ProfileOptions, completionRequest.ConversationId, streaming: false);
                completion.ToolExecutionResponses.AddRange(toolExecutionResponses);
            }
            return completion;
        }

        private async Task<List<Message>> BuildAndUpdateMessageHistory(Guid conversationId, List<Message> requestMessages, int? maxMessageHistory = null)
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

        public async Task<Profile> BuildCompletionOptions(Profile profile, Profile profileOptions)
        {
            // adds the profile references to the tools
            if (profileOptions.Tools == null) profileOptions.Tools = new List<Tool>();
            if (profileOptions.Reference_Profiles != null) profileOptions.Tools.AddRange(await BuildProfileReferenceTool(profileOptions.Reference_Profiles));

            return new Profile()
            {
                Id = profile.Id,
                Name = profile.Name,
                Model = profileOptions.Model ?? profile.Model,
                RagDatabase = profileOptions.RagDatabase ?? profile.RagDatabase,
                MaxMessageHistory = profileOptions.MaxMessageHistory ?? profile.MaxMessageHistory,
                Max_Tokens = profileOptions.Max_Tokens ?? profile.Max_Tokens,
                Temperature = profileOptions.Temperature ?? profile.Temperature,
                Top_P = profileOptions.Top_P ?? profile.Top_P,
                Frequency_Penalty = profileOptions.Frequency_Penalty ?? profile.Frequency_Penalty,
                Presence_Penalty = profileOptions.Presence_Penalty ?? profile.Presence_Penalty,
                Stop = profileOptions.Stop ?? profile.Stop,
                Logprobs = profileOptions.Logprobs ?? profile.Logprobs,
                Top_Logprobs = profileOptions.Top_Logprobs ?? profile.Top_Logprobs,
                Response_Format = profileOptions.Response_Format ?? profile.Response_Format,
                User = profileOptions.User ?? profile.User,
                Tools = profileOptions.Tools ?? profile.Tools,
                System_Message = profileOptions.System_Message ?? profile.System_Message,
                Return_Recursion = profileOptions.Return_Recursion ?? profile.Return_Recursion,
                Reference_Profiles = profileOptions.Reference_Profiles ?? profile.Reference_Profiles,
            };
        }
        #endregion

        #region Shared

        private async Task<List<Tool>> BuildProfileReferenceTool(string[] profileNames)
        {
            var tools = new List<Tool>();
            foreach (var name in profileNames)
            {
                var profile = await _profileDb.GetByNameAsync(name);
                if (profile == null) return null;
                tools.Add(new ProfileReferenceTools(profile.Name, profile.ReferenceDescription));
            }
            return tools;
        }

        // seperate this into two functions probably
        public async Task<List<HttpResponseMessage>> ExecuteTools(Dictionary<string, string> toolCalls, List<Message> messages, Profile? options = null, Guid? conversationId = null, bool streaming = false)// ChatCompletion
        {
            var modelRecursionTools = new Dictionary<string, string>();
            var functionResults = new List<HttpResponseMessage>();

            // Handle standard tool calls and collect recursive calls
            foreach (var tool in toolCalls)
            {
                var toolName = tool.Key.Replace("_Reference_AI_Model", "");
                if (tool.Key.Contains("_Reference_AI_Model"))
                {
                    modelRecursionTools.Add(toolName, tool.Value);
                }
                else
                {
                    var dbTool = await _toolDb.GetByNameAsync(toolName);

                    if (!string.IsNullOrEmpty(dbTool.ExecutionUrl))
                    {
                        functionResults.Add(await _ToolClient.CallFunction(tool.Key, tool.Value, dbTool.ExecutionUrl, dbTool.ExecutionMethod, dbTool.ExecutionBase64Key));
                    }
                }
            }

            // Handle recursive completions - i.e. have other models add a message before responding
            foreach (var tool in modelRecursionTools)
            {
                var completionRequest = new CompletionRequest()
                {
                    ConversationId = conversationId,
                    Messages = messages,
                    ProfileOptions = options ?? new Profile() { Name = tool.Key }
                };

                // add anything the previous model wanted to forward
                if (!string.IsNullOrEmpty(tool.Value)) completionRequest.Messages.Add(new Message() { Role = Role.Assistant, Content = tool.Value });
                if (streaming)
                {
                    await foreach (var _ in StreamCompletion(completionRequest)) ;
                }
                else await ProcessCompletion(completionRequest);
            }
            return functionResults;
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

            completion.Content = $"\n\nIf pertinent, use the below documents, each of which is delimited with triple backticks, " +
                $"to respond to assist with your response to the following prompt, and provide their sources in your response if you use them: " +
                $"{completion.Content}\n\n"
                + ragDataString;

            return completion;
        }
        #endregion
    }
}
