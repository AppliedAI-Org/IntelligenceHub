using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using Microsoft.Extensions.Options;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Common.Config;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class ValidationHandlerTests
    {
        private readonly ValidationHandler _handler;
        private readonly Mock<IOptionsMonitor<Settings>> _mockSettings;

        public ValidationHandlerTests()
        {
            // Setup dummy settings with valid models for Azure
            var settings = new Settings
            {
                // Assumed valid AGI models (in lowercase) for testing purposes.
                ValidAGIModels = new string[] { "gpt-4o", "gpt-3.5" }
            };

            _mockSettings = new Mock<IOptionsMonitor<Settings>>();
            _mockSettings.Setup(x => x.CurrentValue).Returns(settings);

            _handler = new ValidationHandler(_mockSettings.Object);
        }

        #region ValidateChatRequest Tests

        [Fact]
        public void ValidateChatRequest_WithValidRequest_ReturnsNull()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "TestProfile",
                Model = "gpt-4o",
                Host = AGIServiceHosts.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text",
                ReferenceProfiles = new string[] { "ref1" },
                Tools = new List<API.DTOs.Tools.Tool>()
            };

            // Note: Based on ValidateMessageList, we assume that a valid request
            // should have at least one message that is not a Role.User (as per your sample logic).
            var message = new Message
            {
                Role = Role.User,
                Content = "Test content",
                User = "Tester"
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = new List<Message> { message }
            };

            // Act
            var result = _handler.ValidateChatRequest(chatRequest);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateChatRequest_MissingProfileName_ReturnsError()
        {
            // Arrange: Profile name is missing (null)
            var profile = new Profile
            {
                Name = null,
                Model = "gpt-4o",
                Host = AGIServiceHosts.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var message = new Message
            {
                Role = Role.Assistant,
                Content = "Test content",
                User = "Tester"
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = new List<Message> { message }
            };

            // Act
            var result = _handler.ValidateChatRequest(chatRequest);

            // Assert
            Assert.Equal("A profile name must be included in the request body or route.", result);
        }

        [Fact]
        public void ValidateChatRequest_WithEmptyMessages_ReturnsError()
        {
            // Arrange: Create a valid profile but empty messages list
            var profile = new Profile
            {
                Name = "TestProfile",
                Model = "gpt-4o",
                Host = AGIServiceHosts.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            var chatRequest = new CompletionRequest
            {
                ProfileOptions = profile,
                Messages = new List<Message>() // empty list
            };

            // Act
            var result = _handler.ValidateChatRequest(chatRequest);

            // Assert
            Assert.Equal("The messages array was null or empty.", result);
        }

        #endregion

        #region ValidateAPIProfile Tests

        [Fact]
        public void ValidateAPIProfile_WithValidProfile_ReturnsNull()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "ValidProfile",
                Model = "gpt-4o",
                Host = AGIServiceHosts.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateAPIProfile(profile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateAPIProfile_WithNameAll_ReturnsError()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "all",
                Model = "gpt-4o",
                Host = AGIServiceHosts.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 50,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateAPIProfile(profile);

            // Assert
            Assert.Equal("Profile name 'all' conflicts with the profile/get/all route.", result);
        }

        #endregion

        #region ValidateProfileOptions Tests

        [Fact]
        public void ValidateProfileOptions_WithValidOptions_ReturnsNull()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "Profile1",
                Model = "gpt-4o",
                Host = AGIServiceHosts.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 30,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateProfileOptions(profile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateProfileOptions_MissingModel_ReturnsError()
        {
            // Arrange: Missing model field
            var profile = new Profile
            {
                Name = "Profile1",
                Model = null,
                Host = AGIServiceHosts.Azure,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                Temperature = 1,
                TopP = 0.9f,
                MaxTokens = 30,
                TopLogprobs = 0,
                ResponseFormat = "text"
            };

            // Act
            var result = _handler.ValidateProfileOptions(profile);

            // Assert
            Assert.Equal("The model parameter is required.", result);
        }

        #endregion

        #region ValidateTool Tests

        [Fact]
        public void ValidateTool_WithValidTool_ReturnsNull()
        {
            // Arrange - Construct a valid tool DTO.
            var tool = new API.DTOs.Tools.Tool
            {
                Function = new API.DTOs.Tools.Function
                {
                    Name = "TestTool",
                    Description = "A test tool",
                    Parameters = new API.DTOs.Tools.Parameters
                    {
                        required = new string[] { "prop1" },
                        properties = new Dictionary<string, API.DTOs.Tools.Property>
                        {
                            {
                                "prop1", new API.DTOs.Tools.Property
                                {
                                    type = "string",
                                    description = "A required property"
                                }
                            }
                        }
                    }
                },
                ExecutionUrl = "http://example.com",
                ExecutionBase64Key = "key",
                ExecutionMethod = "GET"
            };

            // Act
            var result = _handler.ValidateTool(tool);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateTool_MissingFunctionName_ReturnsError()
        {
            // Arrange - Tool with null function name.
            var tool = new API.DTOs.Tools.Tool
            {
                Function = new API.DTOs.Tools.Function
                {
                    Name = null,
                    Description = "A test tool",
                    Parameters = new API.DTOs.Tools.Parameters
                    {
                        required = new string[] { },
                        properties = new Dictionary<string, API.DTOs.Tools.Property>()
                    }
                }
            };

            // Act
            var result = _handler.ValidateTool(tool);

            // Assert
            Assert.Equal("A function name is required for all tools.", result);
        }

        #endregion

        #region ValidateMessageList and ValidateMessage Tests

        [Fact]
        public void ValidateMessageList_WithValidMessages_ReturnsNull()
        {
            // Arrange: Valid messages list (using a non-User role per current validation logic).
            var message = new Message
            {
                Role = Role.User,
                Content = "Hello world",
                User = "Tester"
            };

            var messages = new List<Message> { message };

            // Act
            var result = _handler.ValidateMessageList(messages);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateMessageList_WithEmptyList_ReturnsError()
        {
            // Arrange: Empty messages list.
            var messages = new List<Message>();

            // Act
            var result = _handler.ValidateMessageList(messages);

            // Assert
            Assert.Equal("The messages array was null or empty.", result);
        }

        [Fact]
        public void ValidateMessage_WithoutContentOrImage_ReturnsError()
        {
            // Arrange: Message with neither content nor image.
            var message = new Message
            {
                Role = Role.Assistant,
                Content = "",
                Base64Image = null,
                User = "Tester"
            };

            // Act
            var result = _handler.ValidateMessage(message);

            // Assert
            Assert.Equal("All messages must contain content or an image.", result);
        }

        [Fact]
        public void ValidateMessage_WithValidContent_ReturnsNull()
        {
            // Arrange: A valid message with content.
            var message = new Message
            {
                Role = Role.Assistant,
                Content = "Some valid content",
                User = "Tester"
            };

            // Act
            var result = _handler.ValidateMessage(message);

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}
