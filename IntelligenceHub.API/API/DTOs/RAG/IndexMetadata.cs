using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.DTOs.RAG
{
    public class IndexMetadata
    {
        public string Name { get; set; }
        public QueryType QueryType { get; set; }
        public TimeSpan? IndexingInterval { get; set; }
        public string? EmbeddingModel { get; set; }
        public int? MaxRagAttachments { get; set; }
        public double? ChunkOverlap { get; set; }
        public bool? GenerateTopic { get; set; }
        public bool? GenerateKeywords { get; set; }
        public bool? GenerateTitleVector { get; set; }
        public bool? GenerateContentVector { get; set; }
        public bool? GenerateTopicVector { get; set; }
        public bool? GenerateKeywordVector { get; set; }
        public IndexScoringProfile? ScoringProfile { get; set; } = new IndexScoringProfile();
    }
}
