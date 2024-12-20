using Microsoft.AspNetCore.Mvc;
using System.Text;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.API.DTOs;
using Newtonsoft.Json;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Authorization;
using IntelligenceHub.Business.Interfaces;
using Microsoft.AspNetCore.Http;


namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class CompletionController : ControllerBase
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly IValidationHandler _validationLogic;

        public CompletionController(ICompletionLogic completionLogic, IValidationHandler validationHandler)
        {
            _completionLogic = completionLogic;
            _validationLogic = validationHandler;
        }

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
