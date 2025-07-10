using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    /// <summary>
    /// Entity model representing a tool definition in the database.
    /// </summary>
    [Table("Tools")]
    public class DbTool
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary>
        /// Primary key for the tool definition.
        /// </summary>
        public int Id { get; set; }
        [Required]
        /// <summary>
        /// Name of the tool.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Description explaining the tool's purpose.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Comma-separated list of required fields.
        /// </summary>
        public string Required { get; set; } = string.Empty;
        /// <summary>
        /// URL to invoke when executing the tool.
        /// </summary>
        public string? ExecutionUrl { get; set; }
        /// <summary>
        /// HTTP method used to call the execution URL.
        /// </summary>
        public string? ExecutionMethod { get; set; }
        /// <summary>
        /// Optional base64 encoded API key for invocation.
        /// </summary>
        public string? ExecutionBase64Key { get; set; }
        /// <summary>
        /// Navigation property for associated profiles.
        /// </summary>
        public ICollection<DbProfileTool> ProfileTools { get; set; } = new List<DbProfileTool>();
        /// <summary>
        /// Collection of properties describing tool inputs.
        /// </summary>
        public ICollection<DbProperty> Properties { get; set; } = new List<DbProperty>();
    }
}
