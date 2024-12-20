using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminPolicy")]
    public class MessageHistoryController : ControllerBase
    {
        private readonly IMessageHistoryLogic _messageHistoryLogic;

        public MessageHistoryController(IMessageHistoryLogic messageHistoryLogic)
        {
            _messageHistoryLogic = messageHistoryLogic;
        }

        [HttpGet]
        [Route("conversation/{id}/count/{count}")]
        [ProducesResponseType(typeof(List<Message>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetConversation([FromRoute] Guid id, [FromRoute] int count) // get this to work with either a string or an int
        {
            try
            {
                if (count < 1) return BadRequest("count must be greater than 1");
                var conversation = await _messageHistoryLogic.GetConversationHistory(id, count);
                if (conversation is null || conversation.Count < 1) return NotFound($"The conversation '{id}' does not exist or is empty...");
                else return Ok(conversation);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpPost]
        [Route("conversation/{id}")]
        [ProducesResponseType(typeof(List<Message>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertConversationData([FromRoute] Guid id,[FromBody] List<Message> messages)
        {
            try
            {
                // Validate the messages list
                if (messages == null || messages.Count == 0) return BadRequest("Messages must be included in the request.");

                var responseMessages = await _messageHistoryLogic.UpdateOrCreateConversation(id, messages);
                if (responseMessages is null) return StatusCode(StatusCodes.Status500InternalServerError, $"Something went wrong when adding the messages.");
                else return Ok(responseMessages);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpDelete]
        [Route("conversation/{id}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteConversation([FromRoute] Guid id)
        {
            try
            {
                var response = await _messageHistoryLogic.DeleteConversation(id);
                if (response) return Ok(response);
                else return NotFound($"No conversation with ID '{id}' was found");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpDelete]
        [Route("conversation/{conversationId}/message/{messageId}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMessage(Guid conversationId, [FromRoute] int messageId)
        {
            try
            {
                var response = await _messageHistoryLogic.DeleteMessage(conversationId, messageId);
                if (response) return Ok(response);
                else return NotFound("The conversation or message was not found");
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
