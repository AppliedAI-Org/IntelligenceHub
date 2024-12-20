namespace IntelligenceHub.API.DTOs.RAG
{
    public class IndexMetadata
    {
        public string Name { get; set; }
        public string? QueryType { get; set; }
        public TimeSpan IndexingInterval { get; set; }
        public string? EmbeddingModel { get; set; }
        public int MaxRagAttachments { get; set; }
        public float ChunkOverlap { get; set; } = 0.1f;
        public bool GenerateTopic { get; set; }
        public bool GenerateKeywords { get; set; }
        public bool GenerateTitleVector { get; set; }
        public bool GenerateContentVector { get; set; }
        public bool GenerateTopicVector { get; set; }
        public bool GenerateKeywordVector { get; set; }
        public IndexScoringProfile? ScoringProfile { get; set; } = new IndexScoringProfile();
    }
}
