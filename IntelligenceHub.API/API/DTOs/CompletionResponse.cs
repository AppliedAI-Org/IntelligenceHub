
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    public class CompletionResponse
    {
        public List<Message> Messages { get; set; } // flatten this to just the content string?
        public Dictionary<string, string> ToolCalls { get; set; }
        public List<HttpResponseMessage> ToolExecutionResponses { get; set; }
        public FinishReason? FinishReason { get; set; }
    }
}
