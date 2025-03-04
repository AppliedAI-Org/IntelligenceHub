
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    public class CompletionResponse
    {
        public List<Message> Messages { get; set; }
        public Dictionary<string, string> ToolCalls { get; set; }
        public List<HttpResponseMessage> ToolExecutionResponses { get; set; }
        public FinishReasons? FinishReason { get; set; }
    }
}
