using IntelligenceHub.DAL;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.DAL.Interfaces;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Business.Handlers;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for handling message history.
    /// </summary>
    public class MessageHistoryLogic : IMessageHistoryLogic
    {
        private readonly IMessageHistoryRepository _messageHistoryRepository;
        private readonly IValidationHandler _validationHandler;

        /// <summary>
        /// Constructor for the message history logic class that is resolved by the DI container.
        /// </summary>
        /// <param name="messageRepository">The repository where message history is stored.</param>
        public MessageHistoryLogic(IMessageHistoryRepository messageRepository, IValidationHandler validationHandler)
        {
            _messageHistoryRepository = messageRepository;
            _validationHandler = validationHandler;
        }

        /// <summary>
        /// Gets the conversation history for a given conversation ID.
        /// </summary>
        /// <param name="id">The ID associated with the conversation to be retrieved.</param>
        /// <param name="count">The number of messages to retrieve from the repository.</param>
        /// <param name="page">The number of pages to offset.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{Message}}"/> containing a list of messages associated with the conversation.</returns>
        public async Task<APIResponseWrapper<List<Message>>> GetConversationHistory(Guid id, int count, int page)
        {
            // check if the conversation exists at all in order to return the appropriate error message
            var conversationData = await _messageHistoryRepository.GetConversationAsync(id, 1, 1);
            if (conversationData == null || conversationData.Count < 1) return APIResponseWrapper<List<Message>>.Failure($"A conversation with the id '{id}' was not found.", APIResponseStatusCodes.NotFound);

            var messages = new List<Message>();
            var dbMessages = await _messageHistoryRepository.GetConversationAsync(id, count, page);
            foreach (var dbMessage in dbMessages) messages.Add(DbMappingHandler.MapFromDbMessage(dbMessage));
            return APIResponseWrapper<List<Message>>.Success(messages);
        }

        /// <summary>
        /// Updates or creates a conversation with the given messages.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messages">The messages to add to the conversation history.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{Message}}"/> containing the messages that were successfully added.</returns>
        public async Task<APIResponseWrapper<List<Message>>> UpdateOrCreateConversation(Guid conversationId, List<Message> messages)
        {
            var errorMessage = _validationHandler.ValidateMessageList(messages);
            if (!string.IsNullOrEmpty(errorMessage)) return APIResponseWrapper<List<Message>>.Failure(errorMessage, APIResponseStatusCodes.BadRequest);

            var addedMessages = new List<Message>();
            foreach (var message in messages)
            {
                var dbMessage = DbMappingHandler.MapToDbMessage(message, conversationId);
                var addedMessage = await _messageHistoryRepository.AddAsync(dbMessage);
                if (addedMessage != null) addedMessages.Add(message);
            }
            return APIResponseWrapper<List<Message>>.Success(addedMessages);
        }

        /// <summary>
        /// Deletes a conversation from the repository.
        /// </summary>
        /// <param name="id">The ID of the conversation to delete.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating the success of the operation.</returns>
        public async Task<APIResponseWrapper<bool>> DeleteConversation(Guid id)
        {
            var success = await _messageHistoryRepository.DeleteConversationAsync(id);
            if (success) return APIResponseWrapper<bool>.Success(true);
            return APIResponseWrapper<bool>.Failure($"No conversation with ID '{id}' exists.", APIResponseStatusCodes.NotFound);
        }

        /// <summary>
        /// Adds a single message to the conversation history.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="message">The message to be added to the conversation.</param>
        /// <returns>An <see cref="APIResponseWrapper{DbMessage}"/> containing the new message as it is represented in the database.</returns>
        public async Task<APIResponseWrapper<DbMessage>> AddMessage(Guid conversationId, Message message)
        {
            var dbMessage = DbMappingHandler.MapToDbMessage(message, conversationId);
            var conversation = await _messageHistoryRepository.GetConversationAsync(conversationId, 1, 1);
            if (conversation == null) return APIResponseWrapper<DbMessage>.Failure($"No conversation with the id '{conversationId}' was found", APIResponseStatusCodes.NotFound);
            var updatedDbMessage = await _messageHistoryRepository.AddAsync(dbMessage);
            return APIResponseWrapper<DbMessage>.Success(updatedDbMessage);
        }

        /// <summary>
        /// Deletes a single message from the conversation history.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> representing the success or failure of the operation.</returns>
        public async Task<APIResponseWrapper<bool>> DeleteMessage(Guid conversationId, int messageId)
        {
            var success = await _messageHistoryRepository.DeleteAsync(conversationId, messageId);
            if (success) return APIResponseWrapper<bool>.Success(true);
            return APIResponseWrapper<bool>.Failure($"No conversation with id '{conversationId}' was found.", APIResponseStatusCodes.NotFound);
        }
    }
}
