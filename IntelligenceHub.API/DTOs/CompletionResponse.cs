
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    /// <summary>
    /// Represents the response returned after processing a completion request.
    /// </summary>
    public class CompletionResponse
    {
        /// <summary>
        /// Gets or sets the list of messages returned by the AI model.
        /// </summary>
        public List<Message> Messages { get; set; }

        /// <summary>
        /// Gets or sets any tool calls that were requested by the model.
        /// </summary>
        public Dictionary<string, string> ToolCalls { get; set; }

        /// <summary>
        /// Gets or sets the responses from executed tools.
        /// </summary>
        public List<HttpResponseMessage> ToolExecutionResponses { get; set; }

        /// <summary>
        /// Gets or sets the reason the completion finished.
        /// </summary>
        public FinishReasons? FinishReason { get; set; }
    }
}
