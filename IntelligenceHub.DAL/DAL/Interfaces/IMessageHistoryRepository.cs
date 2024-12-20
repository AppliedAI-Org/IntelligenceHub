using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IMessageHistoryRepository
    {
        Task<List<DbMessage>> GetConversationAsync(Guid conversationId, int count);
        Task<bool> DeleteConversationAsync(Guid conversationId);
        Task<bool> DeleteAsync(Guid conversationId, int messageId);
        Task<DbMessage> AddAsync(DbMessage dbMessage);
    }
}