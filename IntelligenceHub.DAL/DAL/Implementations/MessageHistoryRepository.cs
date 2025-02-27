using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing message history in the database.
    /// </summary>
    public class MessageHistoryRepository : GenericRepository<DbMessage>, IMessageHistoryRepository
    {
        /// <summary>
        /// Constructor for the MessageHistoryRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        public MessageHistoryRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves a conversation by its ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="maxMessages">The maximum number of messages to return.</param>
        /// <returns>A list of messages.</returns>
        public async Task<List<DbMessage>> GetConversationAsync(Guid conversationId, int maxMessages)
        {
            return await _dbSet.Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.TimeStamp)
                .Take(maxMessages)
                .ToListAsync();
        }

        /// <summary>
        /// Deletes a conversation by its ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteConversationAsync(Guid conversationId)
        {
            var messages = await _dbSet.Where(m => m.ConversationId == conversationId).ToListAsync();
            if (messages.Any())
            {
                _dbSet.RemoveRange(messages);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deletes a specific message by its ID.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteAsync(Guid conversationId, int messageId)
        {
            var message = await _dbSet.FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.Id == messageId);
            if (message != null)
            {
                _dbSet.Remove(message);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}