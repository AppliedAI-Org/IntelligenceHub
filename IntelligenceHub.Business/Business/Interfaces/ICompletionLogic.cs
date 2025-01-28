using IntelligenceHub.API.DTOs;

namespace IntelligenceHub.Business.Interfaces
{
    public interface ICompletionLogic
    {
        IAsyncEnumerable<CompletionStreamChunk> StreamCompletion(CompletionRequest completionRequest);
        Task<CompletionResponse?> ProcessCompletion(CompletionRequest completionRequest);
        Task<(List<HttpResponseMessage>, List<Message>)> ExecuteTools(Dictionary<string, string> toolCalls, List<Message> messages, Profile? options = null, Guid? conversationId = null, bool streaming = false, int currentRecursionDepth = 0);
    }
}
