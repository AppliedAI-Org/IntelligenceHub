using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;


namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to send chat requests to the API.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class CompletionController : ControllerBase
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly IValidationHandler _validationLogic;

        /// <summary>
        /// This controller is used to send chat requests to the API.
        /// </summary>
        /// <param name="completionLogic">The business logic for completions.</param>
        /// <param name="validationHandler">A class that validates incoming API request payloads.</param>
        public CompletionController(ICompletionLogic completionLogic, IValidationHandler validationHandler)
        {
            _completionLogic = completionLogic;
            _validationLogic = validationHandler;
        }

        /// <summary>
        /// This endpoint is used to send a chat request to the API.
        /// </summary>
        /// <param name="name">The name of the profile that will be used to construct the request.</param>
        /// <param name="completionRequest">The request body. Only the messages array is required.</param>
        /// <returns>The chat completion response.</returns>
        [HttpPost]
        [Route("Chat/{name}")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompletionStandard([FromRoute] string? name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                completionRequest.ProfileOptions.Name = name ?? completionRequest.ProfileOptions.Name;
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = await _completionLogic.ProcessCompletion(completionRequest);
                if (response is not null) return Ok(response);
                else return BadRequest("Invalid request. Please check your request body.");
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
        /// <returns>The chat completion response.</returns>
        [HttpPost]
        [Route("SSE/{name}")]
        [ProducesResponseType(typeof(CompletionStreamChunk), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompletionStreaming([FromRoute] string? name, [FromBody] CompletionRequest completionRequest)
        {
            try
            {
                completionRequest.ProfileOptions.Name = name ?? completionRequest.ProfileOptions.Name;
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (errorMessage is not null) return BadRequest(errorMessage);
                var response = _completionLogic.StreamCompletion(completionRequest);

                // set headers to return SSE
                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                await foreach (var chunk in response)
                {
                    var jsonChunk = JsonConvert.SerializeObject(chunk);
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
