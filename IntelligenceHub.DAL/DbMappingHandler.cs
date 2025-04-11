using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.DAL.Models;
using System.Text.Json;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.DAL
{
    /// <summary>
    /// Handles mapping between database entities and DTOs and sets defaults where appropriate.
    /// </summary>
    public static class DbMappingHandler
    {
        #region Profiles

        /// <summary>
        /// Maps a database profile entity to a profile DTO.
        /// </summary>
        /// <param name="dbProfile">The database profile entity.</param>
        /// <param name="tools">Optional list of tools associated with the profile.</param>
        /// <returns>A profile DTO.</returns>
        public static Profile MapFromDbProfile(DbProfile dbProfile, List<Tool>? tools = null)
        {
            var profile = new Profile()
            {
                Id = dbProfile.Id,
                Name = dbProfile.Name,
                Model = dbProfile.Model,
                Host = dbProfile.Host.ConvertToServiceHost(),
                ImageHost = dbProfile.ImageHost?.ConvertToServiceHost(), 
                FrequencyPenalty = (float?)dbProfile.FrequencyPenalty,
                PresencePenalty = (float?)dbProfile.PresencePenalty,
                Temperature = (float?)dbProfile.Temperature,
                TopP = (float?)dbProfile.TopP,
                MaxTokens = dbProfile.MaxTokens,
                TopLogprobs = dbProfile.TopLogprobs,
                ResponseFormat = dbProfile.ResponseFormat,
                User = dbProfile.User,
                SystemMessage = dbProfile.SystemMessage,
                Stop = dbProfile.Stop?.ToStringArray(),
                ReferenceProfiles = dbProfile.ReferenceProfiles?.ToStringArray(),
                Tools = tools,
                MaxMessageHistory = dbProfile.MaxMessageHistory,
            };
            profile.Logprobs = profile.TopLogprobs > 0 ? true : false;
            return profile;
        }

        /// <summary>
        /// Maps a profile DTO to a database profile entity.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        /// /// <param name="defaultAzureModel">The default azure model, passed in since this 
        /// class is static, and the value is retrieved from the appsettings.</param>
        /// <param name="existingProfile">Optional existing database profile entity.</param>
        /// <param name="profileUpdate">Optional profile DTO with updated values.</param>
        /// <returns>A database profile entity.</returns>
        public static DbProfile MapToDbProfile(string profileName, string defaultAzureModel, DbProfile? existingProfile = null, Profile? profileUpdate = null)
        {
            if (existingProfile == null)
            {
                existingProfile = new DbProfile();
            }

            var host = profileUpdate?.Host ?? existingProfile.Host.ConvertToServiceHost();
            if (host == AGIServiceHosts.None) host = AGIServiceHosts.OpenAI;

            var model = profileUpdate?.Model ?? existingProfile.Model ?? null;
            if (string.IsNullOrEmpty(model))
            {
                if (host == AGIServiceHosts.Azure) model = defaultAzureModel;
                if (host == AGIServiceHosts.Anthropic) model = DefaultAnthropicModel;
                if (host == AGIServiceHosts.OpenAI) model = DefaultOpenAIModel;
            }

            existingProfile.Name = profileName;
            existingProfile.ResponseFormat = profileUpdate?.ResponseFormat ?? existingProfile.ResponseFormat;
            existingProfile.User = profileUpdate?.User ?? existingProfile.User;
            existingProfile.SystemMessage = profileUpdate?.SystemMessage ?? existingProfile.SystemMessage;
            existingProfile.TopLogprobs = profileUpdate?.TopLogprobs ?? existingProfile.TopLogprobs;
            existingProfile.MaxTokens = profileUpdate?.MaxTokens ?? existingProfile.MaxTokens;
            existingProfile.Model = model;
            existingProfile.Host = host.ToString();
            existingProfile.ImageHost = profileUpdate?.ImageHost.ToString() ?? existingProfile.ImageHost ?? host.ToString();
            existingProfile.FrequencyPenalty = profileUpdate?.FrequencyPenalty ?? existingProfile.FrequencyPenalty ?? 0;
            existingProfile.PresencePenalty = profileUpdate?.PresencePenalty ?? existingProfile.PresencePenalty ?? 0;
            existingProfile.Temperature = profileUpdate?.Temperature ?? existingProfile.Temperature ?? 1;
            existingProfile.TopP = profileUpdate?.TopP ?? existingProfile.TopP ?? 1;
            existingProfile.Stop = profileUpdate?.Stop?.ToCommaSeparatedString() ?? existingProfile.Stop;
            existingProfile.ReferenceProfiles = profileUpdate?.ReferenceProfiles?.ToCommaSeparatedString() ?? existingProfile.ReferenceProfiles;

            return existingProfile;
        }
        #endregion

        #region Tools

        /// <summary>
        /// Maps a database tool entity to a tool DTO.
        /// </summary>
        /// <param name="dbTool">The database tool entity.</param>
        /// <param name="dbProperties">Optional list of database properties associated with the tool.</param>
        /// <returns>A tool DTO.</returns>
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

        /// <summary>
        /// Maps a tool DTO to a database tool entity.
        /// </summary>
        /// <param name="tool">The tool DTO.</param>
        /// <returns>A database tool entity.</returns>
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

        /// <summary>
        /// Maps a property DTO to a database property entity.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="property">The property DTO.</param>
        /// <returns>A database property entity.</returns>
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

        /// <summary>
        /// Maps a database message entity to a message DTO.
        /// </summary>
        /// <param name="dbMessage">The database message entity.</param>
        /// <returns>A message DTO.</returns>
        public static Message MapFromDbMessage(DbMessage dbMessage)
        {
            return new Message()
            {
                Id = dbMessage.Id,
                Content = dbMessage.Content,
                Role = dbMessage.Role.ConvertStringToRole(),
                User = dbMessage.User,
                Base64Image = dbMessage.Base64Image,
                TimeStamp = dbMessage.TimeStamp,
            };
        }

        /// <summary>
        /// Maps a message DTO to a database message entity.
        /// </summary>
        /// <param name="message">The message DTO.</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>A database message entity.</returns>
        public static DbMessage MapToDbMessage(Message message, Guid conversationId)
        {
            return new DbMessage()
            {
                Id = message.Id,
                Content = message.Content,
                Role = message.Role.ToString() ?? string.Empty,
                ConversationId = conversationId,
                User = message.User ?? string.Empty,
                Base64Image = message.Base64Image,
                TimeStamp = message.TimeStamp,
            };
        }
        #endregion
    }
}
