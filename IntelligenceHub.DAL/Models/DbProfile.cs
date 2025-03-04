using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    // extend this from a common DTO?
    [Table("Profiles")]
    public class DbProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string? ImageHost { get; set; }
        public double? FrequencyPenalty { get; set; }
        public double? PresencePenalty { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public int? TopLogprobs { get; set; }
        public int? MaxTokens { get; set; }
        public int? MaxMessageHistory { get; set; }
        public string? ResponseFormat { get; set; }
        public string? User { get; set; }
        public string? SystemMessage { get; set; }
        public string? Stop { get; set; }
        public string? ReferenceProfiles { get; set; }
        public string? ReferenceDescription { get; set; }
        public ICollection<DbProfileTool> ProfileTools { get; set; } = new List<DbProfileTool>();
    }
}
