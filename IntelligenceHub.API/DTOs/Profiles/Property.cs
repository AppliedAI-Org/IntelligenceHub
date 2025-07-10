using Newtonsoft.Json;

namespace IntelligenceHub.API.DTOs.Tools
{
    /// <summary>
    /// Represents a parameter definition for a tool function.
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Gets or sets the database identifier.
        /// </summary>
        [JsonIgnore]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Gets or sets an optional description for the parameter.
        /// </summary>
        public string? description { get; set; }
    }
}
