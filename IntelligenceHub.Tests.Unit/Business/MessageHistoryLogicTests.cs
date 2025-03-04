using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class MessageHistoryTests
    {
        private readonly Mock<IMessageHistoryRepository> _messageHistoryRepositoryMock;
        private readonly MessageHistoryLogic _messageHistoryLogic;

        public MessageHistoryTests()
        {
            _messageHistoryRepositoryMock = new Mock<IMessageHistoryRepository>();
            _messageHistoryLogic = new MessageHistoryLogic(_messageHistoryRepositoryMock.Object);
        }

        [Fact]
        public async Task GetConversationHistory_ShouldReturnMessages()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var timeStamp = DateTime.UtcNow;
            var messages = new List<Message> { new Message { Content = "Test message", TimeStamp = timeStamp } };
            var dbMessages = new List<DbMessage> { new DbMessage { Content = "Test message", TimeStamp = timeStamp } };
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 10, 1)).ReturnsAsync(dbMessages);

            // Act
            var result = await _messageHistoryLogic.GetConversationHistory(conversationId, 10, 1);

            // Assert
            Assert.Collection(result.Data, message =>
            {
                Assert.Equal("Test message", message.Content);
                Assert.Equal(timeStamp, message.TimeStamp);
                Assert.Null(message.Base64Image);
                Assert.Null(message.Role);
            });
        }

        [Fact]
        public async Task GetConversationHistory_ShouldReturnEmptyList_WhenNoMessagesFound()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 10, 1)).ReturnsAsync(new List<DbMessage>());

            // Act
            var result = await _messageHistoryLogic.GetConversationHistory(conversationId, 10, 1);

            // Assert
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task UpdateOrCreateConversation_ShouldAddMessages()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messages = new List<Message> { new Message { Content = "Test message" } };
            var dbMessage = new DbMessage { Content = "Test message" };
            _messageHistoryRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<DbMessage>())).ReturnsAsync(dbMessage);

            // Act
            var result = await _messageHistoryLogic.UpdateOrCreateConversation(conversationId, messages);

            // Assert
            Assert.Single(result.Data);
            Assert.Equal(messages[0].Content, result.Data[0].Content);
        }

        [Fact]
        public async Task UpdateOrCreateConversation_ShouldReturnEmptyList_WhenAddFails()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messages = new List<Message> { new Message { Content = "Test message" } };
            _messageHistoryRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<DbMessage>())).ReturnsAsync((DbMessage?)null);

            // Act
            var result = await _messageHistoryLogic.UpdateOrCreateConversation(conversationId, messages);

            // Assert
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task DeleteConversation_ShouldReturnTrue()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            _messageHistoryRepositoryMock.Setup(repo => repo.DeleteConversationAsync(conversationId)).ReturnsAsync(true);

            // Act
            var result = await _messageHistoryLogic.DeleteConversation(conversationId);

            // Assert
            Assert.True(result.Data);
        }

        [Fact]
        public async Task DeleteConversation_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            _messageHistoryRepositoryMock.Setup(repo => repo.DeleteConversationAsync(conversationId)).ReturnsAsync(false);

            // Act
            var result = await _messageHistoryLogic.DeleteConversation(conversationId);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task AddMessage_ShouldReturnDbMessage()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var message = new Message { Content = "Test message" };
            var dbMessage = new DbMessage { Content = "Test message" };
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 1, 1)).ReturnsAsync(new List<DbMessage> { dbMessage });
            _messageHistoryRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<DbMessage>())).ReturnsAsync(dbMessage);

            // Act
            var result = await _messageHistoryLogic.AddMessage(conversationId, message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dbMessage.Content, result.Data.Content);
        }

        [Fact]
        public async Task AddMessage_ShouldReturnFailure_WhenConversationNotFound()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var message = new Message { Content = "Test message" };
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 1, 1)).ReturnsAsync((List<DbMessage>?)null);

            // Act
            var result = await _messageHistoryLogic.AddMessage(conversationId, message);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal($"No conversation with the id '{conversationId}' was found", result.ErrorMessage);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task DeleteMessage_ShouldReturnTrue()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messageId = 1;
            _messageHistoryRepositoryMock.Setup(repo => repo.DeleteAsync(conversationId, messageId)).ReturnsAsync(true);

            // Act
            var result = await _messageHistoryLogic.DeleteMessage(conversationId, messageId);

            // Assert
            Assert.True(result.Data);
        }

        [Fact]
        public async Task DeleteMessage_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messageId = 1;
            _messageHistoryRepositoryMock.Setup(repo => repo.DeleteAsync(conversationId, messageId)).ReturnsAsync(false);

            // Act
            var result = await _messageHistoryLogic.DeleteMessage(conversationId, messageId);

            // Assert
            Assert.False(result.Data);
        }
    }
}
