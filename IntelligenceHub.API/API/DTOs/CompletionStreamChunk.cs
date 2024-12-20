using System.Text.Json.Serialization;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    public class CompletionStreamChunk
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string CompletionUpdate { get; set; } = string.Empty;
        public string? Base64Image { get; set; }
        public Role? Role { get; set; }
        public FinishReason? FinishReason { get; set; }
        public Dictionary<string, string> ToolCalls { get; set; } = new Dictionary<string, string>();
        public List<HttpResponseMessage> ToolExecutionResponses { get; set; } = new List<HttpResponseMessage>();
        
    }
}
