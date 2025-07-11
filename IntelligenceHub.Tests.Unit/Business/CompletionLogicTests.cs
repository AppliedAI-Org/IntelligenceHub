using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class CompletionLogicTests
    {
        private readonly Mock<IAGIClientFactory> _mockAgiClientFactory;
        private readonly Mock<IAGIClient> _mockAIClient;
        private readonly Mock<IAISearchServiceClient> _mockSearchClient;
        private readonly Mock<IRagClientFactory> _mockRagClientFactory;
        private readonly Mock<IToolClient> _mockToolClient;
        private readonly Mock<IProfileRepository> _mockProfileRepository;
        private readonly Mock<IToolRepository> _mockToolRepository;
        private readonly Mock<IMessageHistoryRepository> _mockMessageHistoryRepository;
        private readonly Mock<IIndexMetaRepository> _mockRagMetaRepository;
        private readonly CompletionLogic _completionLogic;

        public CompletionLogicTests()
        {
            _mockAgiClientFactory = new Mock<IAGIClientFactory>();
            _mockAIClient = new Mock<IAGIClient>();
            _mockSearchClient = new Mock<IAISearchServiceClient>();
            _mockRagClientFactory = new Mock<IRagClientFactory>();
            _mockRagClientFactory.Setup(f => f.GetClient(It.IsAny<RagServiceHost?>())).Returns(_mockSearchClient.Object);
            _mockToolClient = new Mock<IToolClient>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockToolRepository = new Mock<IToolRepository>();
            _mockMessageHistoryRepository = new Mock<IMessageHistoryRepository>();
            _mockRagMetaRepository = new Mock<IIndexMetaRepository>();
            _mockAIClient = new Mock<IAGIClient>();

            _mockAgiClientFactory.Setup(factory => factory.GetClient(It.IsAny<AGIServiceHost>())).Returns(_mockAIClient.Object);

            _completionLogic = new CompletionLogic(
                _mockAgiClientFactory.Object,
                _mockRagClientFactory.Object,
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

            var profile = new DbProfile { Name = "TestProfile", Host = AGIServiceHost.Azure.ToString() };
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(profile);

            var completionStreamChunks = new List<CompletionStreamChunk>
            {
                new CompletionStreamChunk { CompletionUpdate = "Update1", Role = Role.Assistant, FinishReason = FinishReasons.Stop }
            }.ToAsyncEnumerable();

            _mockAIClient.Setup(client => client.StreamCompletion(It.IsAny<CompletionRequest>())).Returns(completionStreamChunks);

            var completionLogic = new CompletionLogic(
                _mockAgiClientFactory.Object,
                _mockRagClientFactory.Object,
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
                resultList.Add(chunk.Data);
            }

            Assert.Single(resultList);
            Assert.Equal("Update1", resultList[0].CompletionUpdate);
            Assert.Equal(FinishReasons.Stop, resultList[0].FinishReason);
        }

        [Fact]
        public async Task ProcessCompletion_ReturnsCompletionResponse_WhenValidRequest()
        {
            // Arrange
            var userMessage = new Message { Role = Role.User, Content = "Test message" };

            var completionRequest = new CompletionRequest
            {
                Messages = new List<Message> { userMessage },
                ConversationId = Guid.NewGuid(),
                ProfileOptions = new Profile { Name = "TestProfile", Host = AGIServiceHost.OpenAI, Model = DefaultOpenAIModel }
            };

            var profile = new DbProfile { Name = "TestProfile", Host = AGIServiceHost.OpenAI.ToString(), Model = DefaultOpenAIModel };
            var completionResponse = new CompletionResponse()
            {
                Messages = new List<Message>()
                {
                    userMessage,
                    new Message() { Role = Role.Assistant, Content = "Response message" }
                },
                FinishReason = FinishReasons.Stop
            };

            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(profile);
            _mockAIClient.Setup(client => client.PostCompletion(It.IsAny<CompletionRequest>())).ReturnsAsync(completionResponse);

            // Act
            var result = await _completionLogic.ProcessCompletion(completionRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(completionResponse, result.Data);
        }

        [Fact]
        public async Task ProcessCompletion_ReturnsErrorResponse_WhenRequestIsInvalid()
        {
            // Arrange
            var completionRequest = new CompletionRequest
            {
                ProfileOptions = new Profile(),
                Messages = new List<Message>()
            };

            // Act
            var result = await _completionLogic.ProcessCompletion(completionRequest);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task BuildCompletionOptions_ReturnsUpdatedProfile_WhenCalled()
        {
            // Arrange
            var profile = new Profile();
            var profileOptions = new Profile();
            var dbProfile = new DbProfile();
            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(dbProfile);

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
            var wrappedResponse = await _completionLogic.ExecuteTools(toolCalls, messages);
            var (httpResults, messageResults) = wrappedResponse.Data;

            // Assert
            Assert.NotNull(httpResults);
            Assert.Single(httpResults);
            Assert.Equal(httpResponse, httpResults.First());
            Assert.NotNull(messageResults);
            Assert.Single(messageResults);
        }

        [Fact]
        public async Task ExecuteTools_ReturnsResponses_WhenRecursionIsExecuted()
        {
            // Arrange
            var toolCalls = new Dictionary<string, string> { { SystemTools.Chat_Recursion.ToString(), "{\"responding_ai_model\":\"TestProfile\"}" } };
            var messages = new List<Message> { new Message { Content = "Initial message", Role = Role.User, TimeStamp = DateTime.UtcNow } };
            var httpResponse = new HttpResponseMessage();
            var dbTool = new DbTool { Name = "Tool1", ExecutionUrl = "http://example.com", ExecutionMethod = "POST" };

            var profile = new DbProfile { Name = "TestProfile", Host = AGIServiceHost.Azure.ToString() };
            var completionResponse = new CompletionResponse
            {
                Messages = new List<Message> { new Message { Content = "Recursive response", Role = Role.Assistant, TimeStamp = DateTime.UtcNow } },
                FinishReason = FinishReasons.Stop
            };

            _mockProfileRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(profile);
            _mockAIClient.Setup(client => client.PostCompletion(It.IsAny<CompletionRequest>())).ReturnsAsync(completionResponse);
            _mockToolRepository.Setup(repo => repo.GetByNameAsync(It.IsAny<string>())).ReturnsAsync(dbTool);
            _mockToolClient.Setup(client => client.CallFunction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponse);

            // Act
            var wrappedResponse = await _completionLogic.ExecuteTools(toolCalls, messages, null, null, 1);
            var (httpResults, messageResults) = wrappedResponse.Data;

            // Assert
            Assert.NotNull(httpResults);
            Assert.Empty(httpResults); // No HTTP results expected for recursion
            Assert.NotNull(messageResults);
            Assert.Equal(2, messageResults.Count); // Initial message + recursive response
            Assert.Equal("Initial message", messageResults[0].Content);
            Assert.Equal("Recursive response", messageResults[1].Content);
        }
    }
}