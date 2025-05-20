using System;
using System.Collections.Generic;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Models;
using Xunit;
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
                ToolId = 1,
                type = "string",
                description = "Test Description"
            };

            // Act
            var result = DbMappingHandler.MapToDbProperty("Test Property", property);

            // Assert
            Assert.Equal(property.ToolId, result.ToolId);
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

        [Fact]
        public void MapFromDbIndexDocument_ShouldMapCorrectly()
        {
            // Arrange
            var dbDocument = new DbIndexDocument
            {
                Id = 1,
                Title = "Test title",
                Content = "Test Content",
                Topic = "Test topic",
                Keywords = "Test keywords",
                Source = "Test source",
                Created = DateTimeOffset.Now,
                Modified = DateTimeOffset.Now
            };

            // Act
            var result = DbMappingHandler.MapFromDbIndexDocument(dbDocument);

            // Assert
            Assert.Equal(dbDocument.Id, result.Id);
            Assert.Equal(dbDocument.Title, result.Title);
            Assert.Equal(dbDocument.Content, result.Content);
            Assert.Equal(dbDocument.Topic, result.Topic);
            Assert.Equal(dbDocument.Keywords, result.Keywords);
            Assert.Equal(dbDocument.Source, result.Source);
            Assert.Equal(dbDocument.Created, result.Created);
            Assert.Equal(dbDocument.Modified, result.Modified);
        }

        [Fact]
        public void MapToDbIndexDocument_ShouldMapCorrectly()
        {
            // Arrange
            var document = new IndexDocument
            {
                Id = 1,
                Title = "Test title",
                Content = "Test Content",
                Topic = "Test topic",
                Keywords = "Test keywords",
                Source = "Test source",
                Created = DateTimeOffset.Now,
                Modified = DateTimeOffset.Now
            };

            // Act
            var result = DbMappingHandler.MapToDbIndexDocument(document);

            // Assert
            Assert.Equal(document.Id, result.Id);
            Assert.Equal(document.Title, result.Title);
            Assert.Equal(document.Content, result.Content);
            Assert.Equal(document.Topic, result.Topic);
            Assert.Equal(document.Keywords, result.Keywords);
            Assert.Equal(document.Source, result.Source);
            Assert.Equal(document.Created, result.Created);
            Assert.Equal(document.Modified, result.Modified);
        }

        [Fact]
        public void MapFromDbIndexMetadata_ShouldMapCorrectly()
        {
            // Arrange
            var dbIndexData = new DbIndexMetadata
            {
                Name = "Test Name",
                QueryType = "Simple",
                GenerationHost = AGIServiceHosts.Azure.ToString(),
                ChunkOverlap = 0.5,
                IndexingInterval = TimeSpan.FromHours(24),
                MaxRagAttachments = 5,
                EmbeddingModel = "Test Model",
                GenerateTopic = true,
                GenerateKeywords = true,
                GenerateTitleVector = true,
                GenerateContentVector = true,
                GenerateTopicVector = true,
                GenerateKeywordVector = true,
                DefaultScoringProfile = "Test Profile",
                ScoringAggregation = "Sum",
                ScoringInterpolation = "Linear",
                ScoringFreshnessBoost = 1.5,
                ScoringBoostDurationDays = 30,
                ScoringTagBoost = 2.0,
                ScoringWeights = "{\"weight1\": 1.0, \"weight2\": 2.0}"
            };

            // Act
            var result = DbMappingHandler.MapFromDbIndexMetadata(dbIndexData);

            // Assert
            Assert.Equal(dbIndexData.Name, result.Name);
            Assert.Equal(dbIndexData.QueryType, result.QueryType.ToString());
            Assert.Equal(dbIndexData.GenerationHost, result.GenerationHost.ToString());
            Assert.Equal(dbIndexData.ChunkOverlap, result.ChunkOverlap);
            Assert.Equal(dbIndexData.IndexingInterval, result.IndexingInterval);
            Assert.Equal(dbIndexData.MaxRagAttachments, result.MaxRagAttachments);
            Assert.Equal(dbIndexData.EmbeddingModel, result.EmbeddingModel);
            Assert.Equal(dbIndexData.GenerateTopic, result.GenerateTopic);
            Assert.Equal(dbIndexData.GenerateKeywords, result.GenerateKeywords);
            Assert.Equal(dbIndexData.GenerateTitleVector, result.GenerateTitleVector);
            Assert.Equal(dbIndexData.GenerateContentVector, result.GenerateContentVector);
            Assert.Equal(dbIndexData.GenerateTopicVector, result.GenerateTopicVector);
            Assert.Equal(dbIndexData.GenerateKeywordVector, result.GenerateKeywordVector);
            Assert.Equal(dbIndexData.DefaultScoringProfile, result.ScoringProfile.Name);
            Assert.Equal(dbIndexData.ScoringAggregation, result.ScoringProfile.SearchAggregation.ToString());
            Assert.Equal(dbIndexData.ScoringInterpolation, result.ScoringProfile.SearchInterpolation.ToString());
            Assert.Equal(dbIndexData.ScoringFreshnessBoost, result.ScoringProfile.FreshnessBoost);
            Assert.Equal(dbIndexData.ScoringBoostDurationDays, result.ScoringProfile.BoostDurationDays);
            Assert.Equal(dbIndexData.ScoringTagBoost, result.ScoringProfile.TagBoost);
            Assert.Equal(2, result.ScoringProfile.Weights.Count);
        }

        [Fact]
        public void MapToDbIndexMetadata_ShouldMapCorrectly()
        {
            // Arrange
            var indexData = new IndexMetadata
            {
                Name = "Test Name",
                QueryType = QueryType.Simple,
                GenerationHost = AGIServiceHosts.OpenAI,
                ChunkOverlap = 0.5,
                IndexingInterval = TimeSpan.FromHours(24),
                MaxRagAttachments = 5,
                EmbeddingModel = "Test Model",
                GenerateTopic = true,
                GenerateKeywords = true,
                GenerateTitleVector = true,
                GenerateContentVector = true,
                GenerateTopicVector = true,
                GenerateKeywordVector = true,
                ScoringProfile = new IndexScoringProfile
                {
                    Name = "Test Profile",
                    SearchAggregation = SearchAggregation.Sum,
                    SearchInterpolation = SearchInterpolation.Linear,
                    FreshnessBoost = 1.5,
                    BoostDurationDays = 30,
                    TagBoost = 2.0,
                    Weights = new Dictionary<string, double>
                    {
                        { "weight1", 1.5 },
                        { "weight2", 2.5 }
                    }
                }
            };

            // Act
            var result = DbMappingHandler.MapToDbIndexMetadata(indexData);

            // Assert
            Assert.Equal(indexData.Name, result.Name);
            Assert.Equal(indexData.QueryType.ToString(), result.QueryType);
            Assert.Equal(indexData.GenerationHost.ToString(), result.GenerationHost);
            Assert.Equal(indexData.ChunkOverlap, result.ChunkOverlap);
            Assert.Equal(indexData.IndexingInterval, result.IndexingInterval);
            Assert.Equal(indexData.MaxRagAttachments, result.MaxRagAttachments);
            Assert.Equal(indexData.EmbeddingModel, result.EmbeddingModel);
            Assert.Equal(indexData.GenerateTopic, result.GenerateTopic);
            Assert.Equal(indexData.GenerateKeywords, result.GenerateKeywords);
            Assert.Equal(indexData.GenerateTitleVector, result.GenerateTitleVector);
            Assert.Equal(indexData.GenerateContentVector, result.GenerateContentVector);
            Assert.Equal(indexData.GenerateTopicVector, result.GenerateTopicVector);
            Assert.Equal(indexData.GenerateKeywordVector, result.GenerateKeywordVector);
            Assert.Equal(indexData.ScoringProfile.Name, result.DefaultScoringProfile);
            Assert.Equal(indexData.ScoringProfile.SearchAggregation.ToString(), result.ScoringAggregation);
            Assert.Equal(indexData.ScoringProfile.SearchInterpolation.ToString(), result.ScoringInterpolation);
            Assert.Equal(indexData.ScoringProfile.FreshnessBoost, result.ScoringFreshnessBoost);
            Assert.Equal(indexData.ScoringProfile.BoostDurationDays, result.ScoringBoostDurationDays);
            Assert.Equal(indexData.ScoringProfile.TagBoost, result.ScoringTagBoost);
            Assert.Equal("{\"weight1\":1.5,\"weight2\":2.5}", result.ScoringWeights);
        }
    }
}
