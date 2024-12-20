using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IMessageHistoryRepository
    {
        Task<List<Message>> GetConversationAsync(Guid conversationId, int count);
        Task<DbMessage?> AddAsync(DbMessage message, string? table = null);
        Task<bool> DeleteConversationAsync(Guid conversationId);
        Task<bool> DeleteMessageAsync(Guid conversationId, int messageId);
    }
}