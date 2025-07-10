using System.Text.Json.Serialization;

namespace IntelligenceHub.API.DTOs.Tools
{
    /// <summary>
    /// Describes a tool that can be invoked by an AI model.
    /// </summary>
    public class Tool
    {
        // used to assist with system message construction
        [JsonIgnore]
        internal readonly string _stringPropertyType = "string";
        [JsonIgnore]
        internal readonly string _objectPropertyType = "object";

        /// <summary>
        /// Gets or sets the identifier of the tool.
        /// </summary>
        [JsonIgnore]
        public int Id { get; set; }

        /// <summary>
        /// Gets the type of this tool. Currently only "function" is supported.
        /// </summary>
        public string Type { get; private set; } = "function";

        /// <summary>
        /// Gets or sets the function definition for the tool.
        /// </summary>
        public Function Function { get; set; } = new Function();

        /// <summary>
        /// Gets or sets the URL that will be called when executing the tool.
        /// </summary>
        public string? ExecutionUrl { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method used for tool execution.
        /// </summary>
        public string? ExecutionMethod { get; set; }

        /// <summary>
        /// Gets or sets an optional base64 encoded API key used when invoking the tool.
        /// </summary>
        public string? ExecutionBase64Key { get; set; }
    }
}
