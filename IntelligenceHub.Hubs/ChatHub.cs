using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Hubs
{
    /// <summary>
    /// A SignalR hub used to stream chat completions to the client
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly IValidationHandler _validationLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatHub"/> class.
        /// </summary>
        /// <param name="completionLogic">The completion logic used to process the request</param>
        /// <param name="validationHandler">The validation logic used to ensure the request body is valid</param>
        public ChatHub(ICompletionLogic completionLogic, IValidationHandler validationHandler)
        {
            _completionLogic = completionLogic;
            _validationLogic = validationHandler;
        }

        /// <summary>
        /// Sends a completion request to the server, and responds with the completion results.
        /// </summary>
        /// <param name="completionRequest">The completion request's body</param>
        /// <returns>An awaitable task after sending the completion response chunks back to the client</returns>
        public async Task Send(CompletionRequest completionRequest)
        {
            try
            {
                var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    await Clients.Caller.SendAsync("broadcastMessage", errorMessage);
                    return;
                }

                var response = _completionLogic.StreamCompletion(completionRequest);
                await foreach (var chunk in response)
                {
                    if (chunk.IsSuccess) await Clients.Caller.SendAsync("broadcastMessage", chunk.Data);
                    else if (chunk.StatusCode == APIResponseStatusCodes.NotFound) await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                    else if (chunk.StatusCode == APIResponseStatusCodes.TooManyRequests) await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                    else if (chunk.StatusCode == APIResponseStatusCodes.InternalError) await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                    else await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {chunk.StatusCode}. Error message: {chunk.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("broadcastMessage", $"Response Status: {500}. Error message: {DefaultExceptionMessage}");
            }
            
        }
    }
}