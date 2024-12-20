using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IntelligenceHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ICompletionLogic _completionLogic;
        private readonly IValidationHandler _validationLogic;

        public ChatHub(ICompletionLogic completionLogic, IValidationHandler validationHandler)
        {
            _completionLogic = completionLogic;
            _validationLogic = validationHandler;
        }

        public async Task Send(CompletionRequest completionRequest)
        {
            var errorMessage = _validationLogic.ValidateChatRequest(completionRequest);
            if (!string.IsNullOrEmpty(errorMessage)) return;
            var response = _completionLogic.StreamCompletion(completionRequest);
            await foreach (var chunk in response) await Clients.Caller.SendAsync("broadcastMessage", chunk);
        }
    }
}