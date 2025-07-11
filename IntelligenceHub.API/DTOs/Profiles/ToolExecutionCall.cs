namespace IntelligenceHub.API.DTOs.Tools
{
    /// <summary>
    /// Represents a request to execute a tool on behalf of the AI model.
    /// </summary>
    public class ToolExecutionCall
    {
        /// <summary>
        /// Gets or sets the name of the tool to invoke.
        /// </summary>
        public string ToolName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets serialized arguments that should be passed to the tool.
        /// </summary>
        public string? Arguments { get; set; }
    }
}

