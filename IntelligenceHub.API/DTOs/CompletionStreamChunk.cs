using System.Text.Json.Serialization;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs
{
    /// <summary>
    /// Represents a single streamed chunk of a completion response.
    /// </summary>
    public class CompletionStreamChunk
    {
        /// <summary>
        /// Gets or sets an internal identifier for the chunk.
        /// </summary>
        [JsonIgnore]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the partial completion text.
        /// </summary>
        public string CompletionUpdate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a base64 encoded image returned by image generation.
        /// </summary>
        public string? Base64Image { get; set; }

        /// <summary>
        /// Gets or sets the role associated with the response.
        /// </summary>
        public Role? Role { get; set; }

        /// <summary>
        /// Gets or sets the username associated with the message.
        /// </summary>
        [JsonIgnore]
        public string? User { get; set; }

        /// <summary>
        /// Gets or sets the reason the completion stopped streaming.
        /// </summary>
        public FinishReasons? FinishReason { get; set; }

        /// <summary>
        /// Gets or sets any tool calls generated during streaming.
        /// </summary>
        public Dictionary<string, string> ToolCalls { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the responses from executed tools.
        /// </summary>
        public List<HttpResponseMessage> ToolExecutionResponses { get; set; } = new List<HttpResponseMessage>();

    }
}
