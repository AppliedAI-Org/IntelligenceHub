using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Models;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class DbMappingHandlerTests
    {
        [Fact]
        public void MapFromDbProfile_ShouldMapCorrectly()
        {
            // Arrange
            var dbProfile = new DbProfile
            {
                Id = 1,
                Name = "Test Profile",
                Model = "Test Model",
                Host = "Azure",
                ImageHost = "Azure",
                FrequencyPenalty = 0.5,
                PresencePenalty = 0.5,
                Temperature = 0.7,
                TopP = 0.9,
                MaxTokens = 100,
                TopLogprobs = 5,
                ResponseFormat = "Test Format",
                User = "Test User",
                SystemMessage = "Test Message",
                Stop = "Stop1,Stop2",
                ReferenceProfiles = "Ref1,Ref2",
                MaxMessageHistory = 10,
            };

            // Act
            var result = DbMappingHandler.MapFromDbProfile(dbProfile);

            // Assert
            Assert.Equal(dbProfile.Id, result.Id);
            Assert.Equal(dbProfile.Name, result.Name);
            Assert.Equal(dbProfile.Model, result.Model);
            Assert.Equal(dbProfile.Host, result.Host.ToString());
            Assert.Equal(dbProfile.ImageHost, result.ImageHost.ToString());
            Assert.Equal(dbProfile.FrequencyPenalty, result.FrequencyPenalty);
            Assert.Equal(dbProfile.PresencePenalty, result.PresencePenalty);
            Assert.Equal(Math.Round(dbProfile.Temperature ?? 0, 1), Math.Round(result.Temperature ?? 0, 1));
            Assert.Equal(Math.Round(dbProfile.TopP ?? 0, 1), Math.Round(result.TopP ?? 0, 1));
            Assert.Equal(dbProfile.MaxTokens, result.MaxTokens);
            Assert.Equal(dbProfile.TopLogprobs, result.TopLogprobs);
            Assert.Equal(dbProfile.ResponseFormat, result.ResponseFormat);
            Assert.Equal(dbProfile.User, result.User);
            Assert.Equal(dbProfile.SystemMessage, result.SystemMessage);
            Assert.Equal(dbProfile.Stop.Split(','), result.Stop);
            Assert.Equal(dbProfile.ReferenceProfiles.Split(','), result.ReferenceProfiles);
            Assert.Equal(dbProfile.MaxMessageHistory, result.MaxMessageHistory);
        }

        [Fact]
        public void MapToDbProfile_ShouldMapCorrectly()
        {
            // Arrange
            var profileUpdate = new Profile
            {
                Host = AGIServiceHosts.Azure,
                Model = "Test Model",
                ResponseFormat = "Test Format",
                User = "Test User",
                SystemMessage = "Test Message",
                TopLogprobs = 5,
                MaxTokens = 100,
                FrequencyPenalty = 0.5f,
                PresencePenalty = 0.5f,
                Temperature = 0.7f,
                TopP = 0.9f,
                Stop = new[] { "Stop1", "Stop2" },
                ReferenceProfiles = new[] { "Ref1", "Ref2" }
            };

            // Act
            var result = DbMappingHandler.MapToDbProfile("Test Profile", "Default Model", null, profileUpdate);

            // Assert
            Assert.Equal("Test Profile", result.Name);
            Assert.Equal("Test Model", result.Model);
            Assert.Equal(AGIServiceHosts.Azure.ToString(), result.Host);
            Assert.Equal("Test Format", result.ResponseFormat);
            Assert.Equal("Test User", result.User);
            Assert.Equal("Test Message", result.SystemMessage);
            Assert.Equal(5, result.TopLogprobs);
            Assert.Equal(100, result.MaxTokens);
            Assert.Equal(0.5, result.FrequencyPenalty);
            Assert.Equal(0.5, result.PresencePenalty);
            Assert.Equal(0.7, Math.Round(result.Temperature ?? 0, 1));
            Assert.Equal(0.9, Math.Round(result.TopP ?? 0, 1));
            Assert.Equal("Stop1,Stop2", result.Stop);
            Assert.Equal("Ref1,Ref2", result.ReferenceProfiles);
        }

        [Fact]
        public void MapFromDbTool_ShouldMapCorrectly()
        {
            // Arrange
            var dbTool = new DbTool
            {
                Id = 1,
                Name = "Test Tool",
                Description = "Test Description",
                ExecutionUrl = "http://test.com",
                ExecutionMethod = "POST",
                ExecutionBase64Key = "TestKey",
                Required = "Param1,Param2"
            };

            var dbProperties = new List<DbProperty>
            {
                new DbProperty { Id = 1, Name = "Param1", Type = "string", Description = "Test Param 1" },
                new DbProperty { Id = 2, Name = "Param2", Type = "int", Description = "Test Param 2" }
            };

            // Act
            var result = DbMappingHandler.MapFromDbTool(dbTool, dbProperties);

            // Assert
            Assert.Equal(dbTool.Id, result.Id);
            Assert.Equal(dbTool.ExecutionUrl, result.ExecutionUrl);
            Assert.Equal(dbTool.ExecutionMethod, result.ExecutionMethod);
            Assert.Equal(dbTool.ExecutionBase64Key, result.ExecutionBase64Key);
            Assert.Equal(dbTool.Name, result.Function.Name);
            Assert.Equal(dbTool.Description, result.Function.Description);
            Assert.Equal(dbTool.Required.Split(','), result.Function.Parameters.required);
            Assert.Equal(dbProperties.Count, result.Function.Parameters.properties.Count);
        }

        [Fact]
        public void MapToDbTool_ShouldMapCorrectly()
        {
            // Arrange
            var tool = new Tool
            {
                Id = 1,
                ExecutionUrl = "http://test.com",
                ExecutionMethod = "POST",
                ExecutionBase64Key = "TestKey",
                Function = new Function
                {
                    Name = "Test Tool",
                    Description = "Test Description",
                    Parameters = new Parameters
                    {
                        required = new[] { "Param1", "Param2" }
                    }
                }
            };

            // Act
            var result = DbMappingHandler.MapToDbTool(tool);

            // Assert
            Assert.Equal(tool.Id, result.Id);
            Assert.Equal(tool.ExecutionUrl, result.ExecutionUrl);
            Assert.Equal(tool.ExecutionMethod, result.ExecutionMethod);
            Assert.Equal(tool.ExecutionBase64Key, result.ExecutionBase64Key);
            Assert.Equal(tool.Function.Name, result.Name);
            Assert.Equal(tool.Function.Description, result.Description);
            Assert.Equal(string.Join(",", tool.Function.Parameters.required), result.Required);
        }

        [Fact]
        public void MapToDbProperty_ShouldMapCorrectly()
        {
            // Arrange
            var property = new Property
            {
                Id = 1,
                type = "string",
                description = "Test Description"
            };

            // Act
            var result = DbMappingHandler.MapToDbProperty("Test Property", property);

            // Assert
            Assert.Equal(property.Id, result.Id);
            Assert.Equal("Test Property", result.Name);
            Assert.Equal(property.type, result.Type);
            Assert.Equal(property.description, result.Description);
        }

        [Fact]
        public void MapFromDbMessage_ShouldMapCorrectly()
        {
            // Arrange
            var dbMessage = new DbMessage
            {
                Content = "Test Content",
                Role = "User",
                User = "Test User",
                Base64Image = "TestImage",
                TimeStamp = DateTime.Now
            };

            // Act
            var result = DbMappingHandler.MapFromDbMessage(dbMessage);

            // Assert
            Assert.Equal(dbMessage.Content, result.Content);
            Assert.Equal(dbMessage.Role, result.Role.ToString());
            Assert.Equal(dbMessage.User, result.User);
            Assert.Equal(dbMessage.Base64Image, result.Base64Image);
            Assert.Equal(dbMessage.TimeStamp, result.TimeStamp);
        }

        [Fact]
        public void MapToDbMessage_ShouldMapCorrectly()
        {
            // Arrange
            var message = new Message
            {
                Content = "Test Content",
                Role = Role.User,
                User = "Test User",
                Base64Image = "TestImage",
                TimeStamp = DateTime.Now
            };

            var conversationId = Guid.NewGuid();

            // Act
            var result = DbMappingHandler.MapToDbMessage(message, conversationId);

            // Assert
            Assert.Equal(message.Content, result.Content);
            Assert.Equal(message.Role.ToString(), result.Role);
            Assert.Equal(conversationId, result.ConversationId);
            Assert.Equal(message.User, result.User);
            Assert.Equal(message.Base64Image, result.Base64Image);
            Assert.Equal(message.TimeStamp, result.TimeStamp);
        }
    }
}
