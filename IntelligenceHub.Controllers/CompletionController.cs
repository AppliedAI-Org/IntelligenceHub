using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System.Text;
using static IntelligenceHub.Common.GlobalVariables;


namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to send chat requests to the API.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class CompletionController : ControllerBase
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly IProfileLogic _profileLogic;
        private readonly IValidationHandler _validationLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletionController"/> class.
        /// </summary>
        /// <param name="completionLogic">The business logic for completions.</param>
        /// <param name="validationHandler">A class that validates incoming API request payloads.</param>
        public CompletionController(ICompletionLogic completionLogic, IProfileLogic profileLogic, IValidationHandler validationHandler)
        {
            _completionLogic = completionLogic;
            _profileLogic = profileLogic;
            _validationLogic = validationHandler;
        }

        /// <summary>
        /// This endpoint is used to send a chat request to the API.
        /// </summary>
        /// <param name="name">The name of the profile that will be used to construct the request.</param>
        /// <param name="completionRequest">The request body. Only the messages array is required.</param>
        /// <returns>An <see cref="IActionResult"/> containing the chat completion response.</returns>
        [HttpPost]
        [Route("Chat/{name?}")]
        [SwaggerOperation(OperationId = "ChatAsync")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompletionStandard([FromRoute] string? name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                name = name?.Replace("{name}", string.Empty); // come up with a more long term fix for this
                if (!string.IsNullOrEmpty(name)) completionRequest.ProfileOptions.Name = name; 
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = await _completionLogic.ProcessCompletion(completionRequest);
                if (response.IsSuccess) return Ok(response.Data);
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                else if (response.StatusCode == APIResponseStatusCodes.TooManyRequests) return StatusCode(StatusCodes.Status429TooManyRequests, response.ErrorMessage);
                else if (response.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
                return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to send a chat request to the API that returns the result via SSE.
        /// </summary>
        /// <param name="name">The name of the profile that will be used to construct the request.</param>
        /// <param name="completionRequest">The request body. Only the messages array is required.</param>
        /// <returns>An <see cref="IActionResult"/> containing the chat completion response.</returns>
        [HttpPost]
        [Route("SSE/{name?}")]
        [SwaggerOperation(OperationId = "ChatSSEAsync")]
        [ProducesResponseType(typeof(CompletionStreamChunk), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompletionStreaming([FromRoute] string? name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                name = name?.Replace("{name}", string.Empty); // come up with a more long term fix for this
                if (!string.IsNullOrEmpty(name)) completionRequest.ProfileOptions.Name = name;
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = _completionLogic.StreamCompletion(completionRequest);

                // set headers to return SSE
                Response.Headers["Content-Type"] = "text/event-stream";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["Connection"] = "keep-alive";
                await foreach (var chunk in response)
                {
                    var jsonChunk = string.Empty;
                    if (chunk.IsSuccess) jsonChunk = JsonConvert.SerializeObject(chunk.Data);
                    else if (chunk.StatusCode == APIResponseStatusCodes.BadRequest) return BadRequest(chunk.ErrorMessage);
                    else if (chunk.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(chunk.ErrorMessage);
                    else if (chunk.StatusCode == APIResponseStatusCodes.TooManyRequests) return StatusCode(StatusCodes.Status429TooManyRequests, chunk.ErrorMessage);
                    else if (chunk.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, chunk.ErrorMessage);

                    var sseMessage = $"data: {jsonChunk}\n\n";
                    var data = Encoding.UTF8.GetBytes(sseMessage);
                    await Response.Body.WriteAsync(data, 0, data.Length);
                    await Response.Body.FlushAsync();
                }
                return new EmptyResult();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
