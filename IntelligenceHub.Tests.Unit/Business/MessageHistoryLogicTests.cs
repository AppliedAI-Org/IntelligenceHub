using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.API.DTOs;
using Moq;
using Xunit;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.DAL.Interfaces;

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
            var messages = new List<Message> { new Message { Content = "Test message" } };
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 10)).ReturnsAsync(messages);

            // Act
            var result = await _messageHistoryLogic.GetConversationHistory(conversationId, 10);

            // Assert
            Assert.Equal(messages, result);
        }

        [Fact]
        public async Task GetConversationHistory_ShouldReturnEmptyList_WhenNoMessagesFound()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 10)).ReturnsAsync(new List<Message>());

            // Act
            var result = await _messageHistoryLogic.GetConversationHistory(conversationId, 10);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateOrCreateConversation_ShouldAddMessages()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messages = new List<Message> { new Message { Content = "Test message" } };
            var dbMessage = new DbMessage { Content = "Test message" };
            _messageHistoryRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<DbMessage>(), null)).ReturnsAsync(dbMessage);

            // Act
            var result = await _messageHistoryLogic.UpdateOrCreateConversation(conversationId, messages);

            // Assert
            Assert.Single(result);
            Assert.Equal(messages[0].Content, result[0].Content);
        }

        [Fact]
        public async Task UpdateOrCreateConversation_ShouldReturnEmptyList_WhenAddFails()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messages = new List<Message> { new Message { Content = "Test message" } };
            _messageHistoryRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<DbMessage>(), null)).ReturnsAsync((DbMessage?)null);

            // Act
            var result = await _messageHistoryLogic.UpdateOrCreateConversation(conversationId, messages);

            // Assert
            Assert.Empty(result);
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
            Assert.True(result);
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
            Assert.False(result);
        }

        [Fact]
        public async Task AddMessage_ShouldReturnDbMessage()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var message = new Message { Content = "Test message" };
            var dbMessage = new DbMessage { Content = "Test message" };
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 1)).ReturnsAsync(new List<Message> { message });
            _messageHistoryRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<DbMessage>(), null)).ReturnsAsync(dbMessage);

            // Act
            var result = await _messageHistoryLogic.AddMessage(conversationId, message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dbMessage.Content, result.Content);
        }

        [Fact]
        public async Task AddMessage_ShouldReturnNull_WhenConversationNotFound()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var message = new Message { Content = "Test message" };
            _messageHistoryRepositoryMock.Setup(repo => repo.GetConversationAsync(conversationId, 1)).ReturnsAsync((List<Message>?)null);

            // Act
            var result = await _messageHistoryLogic.AddMessage(conversationId, message);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteMessage_ShouldReturnTrue()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messageId = 1;
            _messageHistoryRepositoryMock.Setup(repo => repo.DeleteMessageAsync(conversationId, messageId)).ReturnsAsync(true);

            // Act
            var result = await _messageHistoryLogic.DeleteMessage(conversationId, messageId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteMessage_ShouldReturnFalse_WhenDeleteFails()
        {
            // Arrange
            var conversationId = Guid.NewGuid();
            var messageId = 1;
            _messageHistoryRepositoryMock.Setup(repo => repo.DeleteMessageAsync(conversationId, messageId)).ReturnsAsync(false);

            // Act
            var result = await _messageHistoryLogic.DeleteMessage(conversationId, messageId);

            // Assert
            Assert.False(result);
        }
    }
}
