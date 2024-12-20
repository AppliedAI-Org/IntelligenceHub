using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.Business.Interfaces
{
    public interface IMessageHistoryLogic
    {
        Task<List<Message>> GetConversationHistory(Guid id, int count);
        Task<List<Message>> UpdateOrCreateConversation(Guid conversationId, List<Message> messages);
        Task<bool> DeleteConversation(Guid id);
        Task<DbMessage?> AddMessage(Guid conversationId, Message message);
        Task<bool> DeleteMessage(Guid conversationId, int messageId);
    }
}
