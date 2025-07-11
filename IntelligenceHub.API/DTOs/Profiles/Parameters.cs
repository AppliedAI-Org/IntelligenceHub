
namespace IntelligenceHub.API.DTOs.Tools
{
    /// <summary>
    /// Represents the parameter schema for a function.
    /// </summary>
    public class Parameters
    {
        /// <summary>
        /// Gets or sets the type of the parameters object.
        /// </summary>
        public string type { get; set; } = "object";

        /// <summary>
        /// Gets or sets the collection of named properties.
        /// </summary>
        public Dictionary<string, Property> properties { get; set; } = new Dictionary<string, Property>();

        /// <summary>
        /// Gets or sets a list of required property names.
        /// </summary>
        public string[]? required { get; set; } = new string[] { };
    }
}
