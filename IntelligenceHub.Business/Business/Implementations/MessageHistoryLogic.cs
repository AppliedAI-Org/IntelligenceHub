using IntelligenceHub.DAL;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Interfaces;

namespace IntelligenceHub.Business.Implementations
{
    public class MessageHistoryLogic : IMessageHistoryLogic
    {
        private readonly IMessageHistoryRepository _messageHistoryRepository;

        public MessageHistoryLogic(IMessageHistoryRepository messageRepository)
        {
            _messageHistoryRepository = messageRepository;
        }

        public async Task<List<Message>> GetConversationHistory(Guid id, int count)
        {
            var messages = new List<Message>();
            var dbMessages = await _messageHistoryRepository.GetConversationAsync(id, count);
            foreach (var dbMessage in dbMessages) messages.Add(DbMappingHandler.MapFromDbMessage(dbMessage));
            return messages;
        }

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

        public async Task<bool> DeleteConversation(Guid id)
        {
            return await _messageHistoryRepository.DeleteConversationAsync(id);
        }

        public async Task<DbMessage?> AddMessage(Guid conversationId, Message message)
        {
            var dbMessage = DbMappingHandler.MapToDbMessage(message, conversationId);
            var conversation = await _messageHistoryRepository.GetConversationAsync(conversationId, 1);
            if (conversation == null) return null;
            return await _messageHistoryRepository.AddAsync(dbMessage);
        }

        public async Task<bool> DeleteMessage(Guid conversationId, int messageId)
        {
            return await _messageHistoryRepository.DeleteAsync(conversationId, messageId);
        }
    }
}
