using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Business logic for handling message history.
    /// </summary>
    public interface IMessageHistoryLogic
    {
        /// <summary>
        /// Gets the conversation history for a given conversation ID.
        /// </summary>
        /// <param name="id">The ID associated with the conversation to be retrieved.</param>
        /// <param name="count">The number of messages to retrieve from the repository.</param>
        /// <param name="page">The number of pages to offset</param>
        /// <returns>A list of messages assocaited with the conversation.</returns>
        Task<List<Message>> GetConversationHistory(Guid id, int count, int page);

        /// <summary>
        /// Updates or creates a conversation with the given messages.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messages">The messages to add to the conversation history.</param>
        /// <returns>The messages that were successfully added.</returns>
        Task<List<Message>> UpdateOrCreateConversation(Guid conversationId, List<Message> messages);

        /// <summary>
        /// Deletes a conversation from the repository.
        /// </summary>
        /// <param name="id">The ID of the conversation to delete.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteConversation(Guid id);

        /// <summary>
        /// Adds a single message to the conversation history.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="message">The message to be added to the conversation.</param>
        /// <returns>The new message as its represented in the database.</returns>
        Task<DbMessage?> AddMessage(Guid conversationId, Message message);

        /// <summary>
        /// Deletes a single message from the conversation history.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>A boolean representing the success or failure of the operation.</returns>
        Task<bool> DeleteMessage(Guid conversationId, int messageId);
    }
}
