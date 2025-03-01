using IntelligenceHub.DAL;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Interfaces;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for handling message history.
    /// </summary>
    public class MessageHistoryLogic : IMessageHistoryLogic
    {
        private readonly IMessageHistoryRepository _messageHistoryRepository;

        /// <summary>
        /// Constructor for the message history logic class that is resolved by the DI container.
        /// </summary>
        /// <param name="messageRepository">The repository where message history is stored.</param>
        public MessageHistoryLogic(IMessageHistoryRepository messageRepository)
        {
            _messageHistoryRepository = messageRepository;
        }

        /// <summary>
        /// Gets the conversation history for a given conversation ID.
        /// </summary>
        /// <param name="id">The ID associated with the conversation to be retrieved.</param>
        /// <param name="count">The number of messages to retrieve from the repository.</param>
        /// <param name="page">The number of pages to offset</param>
        /// <returns>A list of messages assocaited with the conversation.</returns>
        public async Task<List<Message>> GetConversationHistory(Guid id, int count, int page)
        {
            var messages = new List<Message>();
            var dbMessages = await _messageHistoryRepository.GetConversationAsync(id, count, page);
            foreach (var dbMessage in dbMessages) messages.Add(DbMappingHandler.MapFromDbMessage(dbMessage));
            return messages;
        }

        /// <summary>
        /// Updates or creates a conversation with the given messages.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messages">The messages to add to the conversation history.</param>
        /// <returns>The messages that were successfully added.</returns>
        public async Task<List<Message>> UpdateOrCreateConversation(Guid conversationId, List<Message> messages)
        {
            var addedMessages = new List<Message>();
            foreach (var message in messages)
            {
                var dbMessage = DbMappingHandler.MapToDbMessage(message, conversationId);
                var addedMessage = await _messageHistoryRepository.AddAsync(dbMessage);
                if (addedMessage is null) return addedMessages;
                addedMessages.Add(message);
            }
            return addedMessages;
        }

        /// <summary>
        /// Deletes a conversation from the repository.
        /// </summary>
        /// <param name="id">The ID of the conversation to delete.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteConversation(Guid id)
        {
            return await _messageHistoryRepository.DeleteConversationAsync(id);
        }

        /// <summary>
        /// Adds a single message to the conversation history.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="message">The message to be added to the conversation.</param>
        /// <returns>The new message as its represented in the database.</returns>
        public async Task<DbMessage?> AddMessage(Guid conversationId, Message message)
        {
            var dbMessage = DbMappingHandler.MapToDbMessage(message, conversationId);
            var conversation = await _messageHistoryRepository.GetConversationAsync(conversationId, 1, 1);
            if (conversation == null) return null;
            return await _messageHistoryRepository.AddAsync(dbMessage);
        }

        /// <summary>
        /// Deletes a single message from the conversation history.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>A boolean representing the success or failure of the operation.</returns>
        public async Task<bool> DeleteMessage(Guid conversationId, int messageId)
        {
            return await _messageHistoryRepository.DeleteAsync(conversationId, messageId);
        }
    }
}
