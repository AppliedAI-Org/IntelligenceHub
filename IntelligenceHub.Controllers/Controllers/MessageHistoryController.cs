using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to manage message history.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    [Authorize(Policy = ElevatedAuthPolicy)]
    public class MessageHistoryController : ControllerBase
    {
        private readonly IMessageHistoryLogic _messageHistoryLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHistoryController"/> class.
        /// </summary>
        /// <param name="messageHistoryLogic">The message history business logic.</param>
        public MessageHistoryController(IMessageHistoryLogic messageHistoryLogic)
        {
            _messageHistoryLogic = messageHistoryLogic;
        }

        /// <summary>
        /// This endpoint is used to get the conversation history for a given conversation ID.
        /// </summary>
        /// <param name="id">The ID of the conversation.</param>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of messages to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of messages.</returns>
        [HttpGet]
        [Route("conversation/{id}/page/{page}/count/{count}")]
        [SwaggerOperation(OperationId = "GetConversationAsync")]
        [ProducesResponseType(typeof(List<Message>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetConversation([FromRoute] Guid id, [FromRoute] int page, [FromRoute] int count)
        {
            try
            {
                if (count < 1) return BadRequest("Count must be greater than 0.");
                if (page < 1) return BadRequest("Page must be greater than 0.");
                var response = await _messageHistoryLogic.GetConversationHistory(id, count, page);
                if (!response.IsSuccess) return BadRequest(response.ErrorMessage);

                var conversation = response.Data;
                if (conversation is null || conversation.Count < 1) return NotFound($"The conversation '{id}' does not exist or is empty.");
                else return Ok(conversation);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to add messages to the conversation history.
        /// </summary>
        /// <param name="id">The ID of the conversation.</param>
        /// <param name="messages">The list of messages to add to the conversation.</param>
        /// <returns>An <see cref="IActionResult"/> containing the newly added messages.</returns>
        [HttpPost]
        [Route("conversation/{id}")]
        [SwaggerOperation(OperationId = "UpsertConversationAsync")]
        [ProducesResponseType(typeof(List<Message>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertConversationData([FromRoute] Guid id, [FromBody] List<Message> messages)
        {
            try
            {
                // Validate the messages list
                if (messages == null || !messages.Any()) return BadRequest("Messages must be included in the request.");

                var response = await _messageHistoryLogic.UpdateOrCreateConversation(id, messages);
                if (!response.IsSuccess) return BadRequest(response.ErrorMessage);

                var responseMessages = response.Data;
                if (responseMessages is null) return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong when adding the messages.");
                else return Ok(responseMessages);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to delete a conversation from the repository.
        /// </summary>
        /// <param name="id">The ID of the conversation.</param>
        /// <returns>An empty <see cref="IActionResult"/>.</returns>
        [HttpDelete]
        [Route("conversation/{id}")]
        [SwaggerOperation(OperationId = "DeleteConversationAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteConversation([FromRoute] Guid id)
        {
            try
            {
                var response = await _messageHistoryLogic.DeleteConversation(id);
                if (response.IsSuccess) return NoContent();
                return NotFound(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to delete a message from the conversation history.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="messageId">The ID of the message.</param>
        /// <returns>An empty <see cref="IActionResult"/>.</returns>
        [HttpDelete]
        [Route("conversation/{conversationId}/message/{messageId}")]
        [SwaggerOperation(OperationId = "DeleteMessageAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMessage([FromRoute] Guid conversationId, [FromRoute] int messageId)
        {
            try
            {
                var response = await _messageHistoryLogic.DeleteMessage(conversationId, messageId);
                if (response.IsSuccess) return NoContent();
                else return NotFound(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
