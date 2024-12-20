using Newtonsoft.Json;
using IntelligenceHub.Common.Attributes; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    // extend this from a common DTO?
    [TableName("Profiles")]
    public class DbProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Model { get; set; }
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? TopLogprobs { get; set; }
        public int? MaxTokens { get; set; }
        public int? MaxMessageHistory { get; set; }
        public string? ResponseFormat { get; set; }
        public string? User { get; set; }
        public string? SystemMessage { get; set; }
        public string? Stop { get; set; }
        public string? ReferenceProfiles { get; set; }
        public string? ReferenceDescription { get; set; }
        public bool? ReturnRecursion { get; set; }
    }
}
