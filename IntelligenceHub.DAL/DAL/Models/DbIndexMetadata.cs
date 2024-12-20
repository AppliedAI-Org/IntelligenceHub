using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Common.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntelligenceHub.DAL.Models
{
    // map both the ScoringProfile and the IndexMetadata here
    [TableName("IndexMetadata")]
    public class DbIndexMetadata
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? QueryType { get; set; }
        [Required]
        public TimeSpan IndexingInterval { get; set; }
        public string? EmbeddingModel { get; set; }
        public int MaxRagAttachments { get; set; } = 3;
        public float ChunkOverlap { get; set; } = 0.1f;
        public bool GenerateTopic { get; set; }
        public bool GenerateKeywords { get; set; }
        public bool GenerateTitleVector { get; set; }
        public bool GenerateContentVector { get; set; }
        public bool GenerateTopicVector { get; set; }
        public bool GenerateKeywordVector { get; set; }
        public string? DefaultScoringProfile { get; set; }
        public string? ScoringAggregation { get; set; }
        public string? ScoringInterpolation { get; set; }
        public double ScoringFreshnessBoost { get; set; }
        public int ScoringBoostDurationDays { get; set; }
        public double ScoringTagBoost { get; set; }
        public string? ScoringWeights { get; set; } // serialized Dictioanry<string, double> as json
    }
}
