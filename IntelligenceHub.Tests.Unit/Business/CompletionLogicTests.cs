using Moq;
using System.Linq;
using System.Collections.Generic;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.DAL.Models;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.DAL.Interfaces;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class CompletionLogicTests
    {
        private readonly Mock<IAGIClient> _mockAIClient;
        private readonly Mock<IAISearchServiceClient> _mockSearchClient;
        private readonly Mock<IToolClient> _mockToolClient;
        private readonly Mock<IProfileRepository> _mockProfileRepository;
        private readonly Mock<IToolRepository> _mockToolRepository;
        private readonly Mock<IMessageHistoryRepository> _mockMessageHistoryRepository;
        private readonly Mock<IIndexMetaRepository> _mockRagMetaRepository;
        private readonly CompletionLogic _completionLogic;

        public CompletionLogicTests()
        {
            _mockAIClient = new Mock<IAGIClient>();
            _mockSearchClient = new Mock<IAISearchServiceClient>();
            _mockToolClient = new Mock<IToolClient>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockToolRepository = new Mock<IToolRepository>();
            _mockMessageHistoryRepository = new Mock<IMessageHistoryRepository>();
            _mockRagMetaRepository = new Mock<IIndexMetaRepository>();

            _completionLogic = new CompletionLogic(
                _mockAIClient.Object,
                _mockSearchClient.Object,
                _mockToolClient.Object,
                _mockToolRepository.Object,
                _mockProfileRepository.Object,
                _mockMessageHistoryRepository.Object,
                _mockRagMetaRepository.Object
            );
        }

        [Fact]
        public async Task StreamCompletion_ShouldReturnCompletionStreamChunks()
        {
            var completionRequest = new CompletionRequest
            {
                ProfileOptions = new Profile { Name = "TestProfile" },
                ConversationId = Guid.NewGuid(),
                Messages = new List<Message> { new Message { Content = "Test message", Role = Role.User, TimeStamp = DateTime.UtcNow } }
            };

            var profile = new Profile { Name = "TestProfile" };
            _mockProfileRepository.Setup(repo => repo.GetByNameWithToolsAsync(It.IsAny<string>())).ReturnsAsync(profile);

            var completionStreamChunks = new List<CompletionStreamChunk>
            {
                new CompletionStreamChunk { CompletionUpdate = "Update1", Role = Role.Assistant, FinishReason = FinishReason.Stop }
            }.ToAsyncEnumerable();

            _mockAIClient.Setup(client => client.StreamCompletion(It.IsAny<CompletionRequest>())).Returns(completionStreamChunks);

            var completionLogic = new CompletionLogic(
                _mockAIClient.Object,
                _mockSearchClient.Object,
                _mockToolClient.Object,
                _mockToolRepository.Object,
                _mockProfileRepository.Object,
                _mockMessageHistoryRepository.Object,
                _mockRagMetaRepository.Object
            );

            // Act
            var result = completionLogic.StreamCompletion(completionRequest);

            // Assert
            var resultList = new List<CompletionStreamChunk>();
            await foreach (var chunk in result)
            {
                resultList.Add(chunk);
            }

            Assert.Single(resultList);
            Assert.Equal("Update1", resultList[0].CompletionUpdate);
            Assert.Equal(FinishReason.Stop, resultList[0].FinishReason);
        }

        [Fact]
        public async Task ProcessCompletion_ReturnsCompletionResponse_WhenValidRequest()
        {
            // Arrange
            var completionRequest = new CompletionRequest
            {
                Messages = new List<Message> { new Message { Content = "Test message" } },
                ProfileOptions = new Profile { Name = "TestProfile" }
            };

            var profile = new Profile { Name = "TestProfile" };
            var completionResponse = new CompletionResponse { FinishReason = FinishReason.Stop };

            _mockProfileRepository.Setup(repo => repo.GetByNameWithToolsAsync(It.IsAny<string>())).ReturnsAsync(profile);
            _mockAIClient.Setup(client => client.PostCompletion(It.IsAny<CompletionRequest>())).ReturnsAsync(completionResponse);

            // Act
            var result = await _completionLogic.ProcessCompletion(completionRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(completionResponse, result);
        }

        [Fact]
        public async Task ProcessCompletion_ReturnsErrorResponse_WhenRequestIsInvalid()
        {
            // Arrange
            var completionRequest = new CompletionRequest();
            _mockAIClient.Setup(client => client.PostCompletion(It.IsAny<CompletionRequest>()))
                .ReturnsAsync((CompletionResponse)null);

            // Act
            var result = await _completionLogic.ProcessCompletion(completionRequest);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task BuildCompletionOptions_ReturnsUpdatedProfile_WhenCalled()
        {
            // Arrange
            var profile = new Profile();
            var profileOptions = new Profile();
            _mockProfileRepository.Setup(repo => repo.GetByNameWithToolsAsync(It.IsAny<string>()))
                .ReturnsAsync(profile);

            // Act
            var result = await _completionLogic.BuildCompletionOptions(profile, profileOptions);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ExecuteTools_ReturnsResponses_WhenToolsAreExecuted()
        {
            // Arrange
            var toolCalls = new Dictionary<string, string> { { "Tool1", "Args1" } };
            var messages = new List<Message> { new Message() };
            var httpResponse = new HttpResponseMessage();
            var dbTool = new DbTool { Name = "Tool1", ExecutionUrl = "http://example.com", ExecutionMethod = "POST" };

            _mockToolRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(dbTool);

            _mockToolClient.Setup(client => client.CallFunction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _completionLogic.ExecuteTools(toolCalls, messages);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(httpResponse, result.First());
        }
    }
}