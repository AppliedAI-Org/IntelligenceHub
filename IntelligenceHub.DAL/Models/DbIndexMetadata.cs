using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.DAL.Models
{
    // map both the ScoringProfile and the IndexMetadata here
    /// <summary>
    /// Entity model representing metadata for a RAG index stored in the database.
    /// </summary>
    [Table("IndexMetadata")]
    public class DbIndexMetadata
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary>
        /// Primary key for the index metadata record.
        /// </summary>
        public int Id { get; set; }
        [Required]
        /// <summary>
        /// Gets or sets the name of the index.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the query type used when searching this index.
        /// </summary>
        public string? QueryType { get; set; }
        [Required]
        /// <summary>
        /// Host URL used for generating embeddings.
        /// </summary>
        public string GenerationHost { get; set; } = string.Empty;
        [Required]
        /// <summary>
        /// Host URL for RAG related operations.
        /// </summary>
        public string RagHost { get; set; } = string.Empty;
        [Required]
        /// <summary>
        /// How frequently the index should be refreshed.
        /// </summary>
        public TimeSpan IndexingInterval { get; set; }
        /// <summary>
        /// The embedding model to use for vectorization.
        /// </summary>
        public string? EmbeddingModel { get; set; }
        /// <summary>
        /// Maximum number of attachments to include in a RAG response.
        /// </summary>
        public int? MaxRagAttachments { get; set; }
        /// <summary>
        /// Amount of text overlap between chunks when splitting content.
        /// </summary>
        public double? ChunkOverlap { get; set; }
        /// <summary>
        /// When true, a topic is generated for each document.
        /// </summary>
        public bool GenerateTopic { get; set; }
        /// <summary>
        /// When true, keywords are generated for each document.
        /// </summary>
        public bool GenerateKeywords { get; set; }
        /// <summary>
        /// When true, a vector for the document title is generated.
        /// </summary>
        public bool GenerateTitleVector { get; set; }
        /// <summary>
        /// When true, a vector for the document content is generated.
        /// </summary>
        public bool GenerateContentVector { get; set; }
        /// <summary>
        /// When true, a vector for the generated topic is stored.
        /// </summary>
        public bool GenerateTopicVector { get; set; }
        /// <summary>
        /// When true, a vector for generated keywords is stored.
        /// </summary>
        public bool GenerateKeywordVector { get; set; }
        /// <summary>
        /// Name of the default scoring profile to apply.
        /// </summary>
        public string? DefaultScoringProfile { get; set; }
        /// <summary>
        /// Aggregation strategy used when combining scores.
        /// </summary>
        public string? ScoringAggregation { get; set; }
        /// <summary>
        /// Interpolation strategy used for scoring.
        /// </summary>
        public string? ScoringInterpolation { get; set; }
        /// <summary>
        /// Boost applied to recent documents.
        /// </summary>
        public double? ScoringFreshnessBoost { get; set; }
        /// <summary>
        /// Duration in days for which freshness boost applies.
        /// </summary>
        public int? ScoringBoostDurationDays { get; set; }
        /// <summary>
        /// Boost applied to tags.
        /// </summary>
        public double? ScoringTagBoost { get; set; }
        /// <summary>
        /// Serialized dictionary of custom scoring weights.
        /// </summary>
        public string? ScoringWeights { get; set; } // serialized Dictionary<string, double> as json
    }
}
