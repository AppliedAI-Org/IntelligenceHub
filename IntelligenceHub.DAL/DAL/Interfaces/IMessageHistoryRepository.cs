using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing message history in the database.
    /// </summary>
    public interface IMessageHistoryRepository
    {
        /// <summary>
        /// Retrieves a conversation by its ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="maxMessages">The maximum number of messages to return.</param>
        /// /// <param name="pageNumber">The number of pages to offset</param>
        /// <returns>A list of messages.</returns>
        Task<List<DbMessage>> GetConversationAsync(Guid conversationId, int maxMessages, int pageNumber);

        /// <summary>
        /// Deletes a conversation by its ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteConversationAsync(Guid conversationId);

        /// <summary>
        /// Adds a new message to the conversation.
        /// </summary>
        /// <param name="dbMessage">The message to add.</param>
        /// <returns>The newly added message.</returns>
        Task<DbMessage> AddAsync(DbMessage dbMessage);

        /// <summary>
        /// Deletes a specific message by its ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteAsync(Guid conversationId, int messageId);
    }
}