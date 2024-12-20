using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    public class MessageHistoryRepository : GenericRepository<DbMessage>, IMessageHistoryRepository
    {
        public MessageHistoryRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        public async Task<List<DbMessage>> GetConversationAsync(Guid conversationId, int maxMessages)
        {
            return await _dbSet.Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.TimeStamp)
                .Take(maxMessages)
                .ToListAsync();
        }

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