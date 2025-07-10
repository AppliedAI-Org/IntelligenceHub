
namespace IntelligenceHub.API.DTOs.Tools
{
    /// <summary>
    /// Defines the schema for a callable function tool.
    /// </summary>
    public class Function
    {
        /// <summary>
        /// Gets or sets the unique name of the function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a human readable description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the parameters accepted by the function.
        /// </summary>
        public Parameters Parameters { get; set; } = new Parameters();
    }
}
