using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    // extend this from a common DTO?
    /// <summary>
    /// Entity model representing an agent profile stored in the database.
    /// </summary>
    [Table("Profiles")]
    public class DbProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary>
        /// Primary key for the profile.
        /// </summary>
        public int Id { get; set; }
        [Required]
        /// <summary>
        /// Name of the profile.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Model name used for completions.
        /// </summary>
        public string Model { get; set; } = string.Empty;
        /// <summary>
        /// Host URI to send completion requests to.
        /// </summary>
        public string Host { get; set; } = string.Empty;
        /// <summary>
        /// Optional name of the backing RAG database.
        /// </summary>
        public string? RagDatabase { get; set; }
        /// <summary>
        /// Optional image generation host.
        /// </summary>
        public string? ImageHost { get; set; }
        /// <summary>
        /// Penalty for repeated tokens.
        /// </summary>
        public double? FrequencyPenalty { get; set; }
        /// <summary>
        /// Penalty encouraging presence of new tokens.
        /// </summary>
        public double? PresencePenalty { get; set; }
        /// <summary>
        /// Temperature parameter for generation.
        /// </summary>
        public double? Temperature { get; set; }
        /// <summary>
        /// Alternative to temperature using nucleus sampling.
        /// </summary>
        public double? TopP { get; set; }
        /// <summary>
        /// Number of logprob values to return.
        /// </summary>
        public int? TopLogprobs { get; set; }
        /// <summary>
        /// Maximum number of tokens to generate.
        /// </summary>
        public int? MaxTokens { get; set; }
        /// <summary>
        /// Number of messages from history to include in the prompt.
        /// </summary>
        public int? MaxMessageHistory { get; set; }
        /// <summary>
        /// Response format, if specified by the model.
        /// </summary>
        public string? ResponseFormat { get; set; }
        /// <summary>
        /// User identifier to send to the model service.
        /// </summary>
        public string? User { get; set; }
        /// <summary>
        /// Optional system prompt for the profile.
        /// </summary>
        public string? SystemMessage { get; set; }
        /// <summary>
        /// Optional stop sequences.
        /// </summary>
        public string? Stop { get; set; }
        /// <summary>
        /// Comma-separated list of reference profile names.
        /// </summary>
        public string? ReferenceProfiles { get; set; }
        /// <summary>
        /// Collection of tool mappings for this profile.
        /// </summary>
        public ICollection<DbProfileTool> ProfileTools { get; set; } = new List<DbProfileTool>();
    }
}
