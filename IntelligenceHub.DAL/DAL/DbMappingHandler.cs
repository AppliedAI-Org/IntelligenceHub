using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.API.DTOs.RAG;
using System.Text.Json;
using IntelligenceHub.Common;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.DAL
{
    public static class DbMappingHandler
    {
        #region Profiles
        public static Profile MapFromDbProfile(DbProfile dbProfile, List<Tool>? tools = null)
        {
            var profile = new Profile()
            {
                Id = dbProfile.Id,
                Name = dbProfile.Name,
                Model = dbProfile.Model,
                Host = dbProfile.Host.ToServiceHost(),
                Frequency_Penalty = (float?)dbProfile.FrequencyPenalty,
                Presence_Penalty = (float?)dbProfile.PresencePenalty,
                Temperature = (float?)dbProfile.Temperature,
                Top_P = (float?)dbProfile.TopP,
                Max_Tokens = dbProfile.MaxTokens,
                Top_Logprobs = dbProfile.TopLogprobs,
                Response_Format = dbProfile.ResponseFormat,
                User = dbProfile.User,
                System_Message = dbProfile.SystemMessage,
                Stop = dbProfile.Stop?.ToStringArray(),
                Reference_Profiles = dbProfile.ReferenceProfiles?.ToStringArray(),
                Tools = tools,
                MaxMessageHistory = dbProfile.MaxMessageHistory,
                ReferenceDescription = dbProfile.ReferenceDescription,
            };
            profile.Logprobs = profile.Top_Logprobs > 0 ? true : false;
            return profile;
        }

        public static DbProfile MapToDbProfile(string profileName, DbProfile? existingProfile = null, Profile? profileUpdate = null)
        {
            return new DbProfile()
            {
                // update or set existing value
                Id = existingProfile?.Id ?? 0,
                Name = profileName, // this value should not be null when this method is called, so it is required as an argument
                ResponseFormat = profileUpdate?.Response_Format ?? existingProfile?.ResponseFormat,
                User = profileUpdate?.User ?? existingProfile?.User,
                SystemMessage = profileUpdate?.System_Message ?? existingProfile?.SystemMessage,
                TopLogprobs = profileUpdate?.Top_Logprobs ?? existingProfile?.TopLogprobs,
                ReferenceDescription = profileUpdate?.ReferenceDescription ?? profileUpdate?.ReferenceDescription ?? string.Empty,
                MaxTokens = profileUpdate?.Max_Tokens ?? existingProfile?.MaxTokens,

                // Variables with default values during first database entry
                Model = profileUpdate?.Model
                    ?? existingProfile?.Model
                    ?? GlobalVariables.DefaultAGIModel,

                Host = profileUpdate?.Host.ToString()
                    ?? existingProfile?.Host.ToString()
                    ?? AGIServiceHosts.OpenAI.ToString(),

                FrequencyPenalty = profileUpdate?.Frequency_Penalty
                    ?? existingProfile?.FrequencyPenalty
                    ?? 0,

                PresencePenalty = profileUpdate?.Presence_Penalty
                    ?? existingProfile?.PresencePenalty
                    ?? 0,

                Temperature = profileUpdate?.Temperature
                    ?? existingProfile?.Temperature
                    ?? 1,

                TopP = profileUpdate?.Top_P
                    ?? existingProfile?.TopP
                    ?? 1,

                Stop = profileUpdate?.Stop?.ToCommaSeparatedString() ?? existingProfile?.Stop,
                ReferenceProfiles = profileUpdate?.Reference_Profiles?.ToCommaSeparatedString() ?? existingProfile?.ReferenceProfiles,
            };
        }
        #endregion

        #region Tools
        public static Tool MapFromDbTool(DbTool dbTool, List<DbProperty>? dbProperties = null)
        {
            var tool = new Tool()
            {
                Id = dbTool.Id,
                ExecutionUrl = dbTool.ExecutionUrl,
                ExecutionMethod = dbTool.ExecutionMethod,
                ExecutionBase64Key = dbTool.ExecutionBase64Key,
                Function = new Function()
                {
                    Name = dbTool.Name,
                    Description = dbTool.Description
                }
            };

            if (dbProperties == null) dbProperties = new List<DbProperty>(); // To prevent null reference exceptions
            foreach (var property in dbProperties) 
            {
                var convertedProp = new Property()
                {
                    Id = property.Id,
                    type = property.Type,
                    description = property.Description,
                };
                tool.Function.Parameters.properties.Add(property.Name, convertedProp);
                tool.Function.Parameters.required = dbTool.Required.ToStringArray();
            }
            return tool;
        }

        public static DbTool MapToDbTool(Tool tool)
        {
            return new DbTool()
            {
                Id = tool.Id,
                Name = tool.Function.Name,
                Description = tool.Function.Description ?? string.Empty,
                Required = tool.Function.Parameters.required?.ToCommaSeparatedString() ?? string.Empty,
                ExecutionUrl = tool.ExecutionUrl,
                ExecutionMethod = tool.ExecutionMethod,
                ExecutionBase64Key = tool.ExecutionBase64Key,
            };
        }

        public static DbProperty MapToDbProperty(string name, Property property)
        {
            return new DbProperty()
            {
                Id = property.Id ?? 0,
                ToolId = property.Id ?? 0,
                Name = name,
                Type = property.type,
                Description = property.description ?? string.Empty,
            };
        }
        #endregion

        #region Messaging
        public static Message MapFromDbMessage(DbMessage dbMessage)
        {
            return new Message()
            {
                Content = dbMessage.Content,
                Role = dbMessage.Role.ConvertStringToRole(),
                User = dbMessage.User,
                Base64Image = dbMessage.Base64Image,
                TimeStamp = dbMessage.TimeStamp,
            };
        }

        public static DbMessage MapToDbMessage(Message message, Guid conversationId)
        {
            return new DbMessage()
            {
                Content = message.Content,
                Role = message.Role.ToString() ?? string.Empty,
                ConversationId = conversationId,
                User = message.User ?? string.Empty,
                Base64Image = message.Base64Image,
                TimeStamp = message.TimeStamp,
            };
        }
        #endregion

        #region RAG Indexing

        public static IndexDocument MapFromDbIndexDocument(DbIndexDocument dbDocument)
        {
            return new IndexDocument()
            {
                Id = dbDocument.Id,
                Title = dbDocument.Title,
                Content = dbDocument.Content,
                Topic = dbDocument.Topic,
                Keywords = dbDocument.Keywords,
                Source = dbDocument.Source,
                Created = dbDocument.Created,
                Modified = dbDocument.Modified
            };
        }

        public static DbIndexDocument MapToDbIndexDocument(IndexDocument document)
        {
            return new DbIndexDocument()
            {
                Id = document.Id,
                Title = document.Title,
                Content = document.Content,
                Topic = document.Topic,
                Keywords = document.Keywords,
                Source = document.Source,
                Created = document.Created,
                Modified = document.Modified
            };
        }

        public static IndexMetadata MapFromDbIndexMetadata(DbIndexMetadata dbIndexData)
        {
            return new IndexMetadata()
            {
                Name = dbIndexData.Name,
                QueryType = dbIndexData.QueryType?.ConvertStringToQueryType() ?? QueryType.Simple,
                GenerationProfile = dbIndexData.GenerationProfile ?? string.Empty,
                ChunkOverlap = dbIndexData.ChunkOverlap ?? DefaultChunkOverlap, // make this a global variable
                IndexingInterval = dbIndexData.IndexingInterval,
                MaxRagAttachments = dbIndexData.MaxRagAttachments ?? DefaultRagAttachmentNumber, // make this a global variable
                EmbeddingModel = dbIndexData.EmbeddingModel,
                GenerateTopic = dbIndexData.GenerateTopic,
                GenerateKeywords = dbIndexData.GenerateKeywords,
                GenerateTitleVector = dbIndexData.GenerateTitleVector,
                GenerateContentVector = dbIndexData.GenerateContentVector,
                GenerateTopicVector = dbIndexData.GenerateTopicVector,
                GenerateKeywordVector = dbIndexData.GenerateKeywordVector,
                ScoringProfile = new IndexScoringProfile()
                {
                    Name = dbIndexData.DefaultScoringProfile ?? string.Empty,
                    SearchAggregation = dbIndexData.ScoringAggregation?.ConvertStringToSearchAggregation(),
                    SearchInterpolation = dbIndexData.ScoringInterpolation?.ConvertStringToSearchInterpolation(),
                    BoostDurationDays = dbIndexData.ScoringBoostDurationDays ?? DefaultScoringBoostDurationDays,
                    FreshnessBoost = dbIndexData.ScoringFreshnessBoost ?? DefaultScoringFreshnessBoost,
                    TagBoost = dbIndexData.ScoringTagBoost ?? DefaultScoringTagBoost,
                    Weights = DeserializeDbWeights(dbIndexData.ScoringWeights) ?? new Dictionary<string, double>()
                }
            };
        }

        public static DbIndexMetadata MapToDbIndexMetadata(IndexMetadata indexData)
        {
            if (indexData.ScoringProfile == null) indexData.ScoringProfile = new IndexScoringProfile();
            var chunkOverlap = indexData.ChunkOverlap;
            return new DbIndexMetadata()
            {
                Name = indexData.Name,
                QueryType = indexData.QueryType.ToString(),
                GenerationProfile = indexData.GenerationProfile ?? string.Empty,
                ChunkOverlap = chunkOverlap ?? DefaultChunkOverlap,
                IndexingInterval = indexData.IndexingInterval ?? TimeSpan.FromHours(23.99), // only slightly under 1 day is supported
                MaxRagAttachments = indexData.MaxRagAttachments ?? DefaultRagAttachmentNumber, // make this a global variable,
                EmbeddingModel = indexData.EmbeddingModel ?? DefaultEmbeddingModel,
                GenerateTopic = indexData.GenerateTopic ?? false,
                GenerateKeywords = indexData.GenerateKeywords ?? false,
                GenerateTitleVector = indexData.GenerateTitleVector ?? true,
                GenerateContentVector = indexData.GenerateContentVector ?? true,
                GenerateTopicVector = indexData.GenerateTopicVector ?? false,
                GenerateKeywordVector = indexData.GenerateKeywordVector ?? false,
                DefaultScoringProfile = indexData.ScoringProfile?.Name,
                ScoringAggregation = indexData.ScoringProfile?.SearchAggregation.ToString(),
                ScoringInterpolation = indexData.ScoringProfile?.SearchInterpolation.ToString(),
                ScoringFreshnessBoost = indexData.ScoringProfile?.FreshnessBoost ?? DefaultScoringFreshnessBoost,
                ScoringBoostDurationDays = indexData.ScoringProfile?.BoostDurationDays ?? DefaultScoringBoostDurationDays,
                ScoringTagBoost = indexData.ScoringProfile?.TagBoost ?? DefaultScoringTagBoost,
                ScoringWeights = indexData.ScoringProfile?.Weights?.Count > 0 ? SerializeDbWeights(indexData.ScoringProfile.Weights) : string.Empty,
            };
        }

        private static Dictionary<string, double>? DeserializeDbWeights(string? serializedWeights)
        {
            if (string.IsNullOrEmpty(serializedWeights)) return null;
            return JsonSerializer.Deserialize<Dictionary<string, double>>(serializedWeights) ?? null;
        }

        private static string SerializeDbWeights(Dictionary<string, double> weights)
        {
            return JsonSerializer.Serialize(weights);
        }

        #endregion
    }
}
