using IntelligenceHub.Common.Attributes;

namespace IntelligenceHub.API.DTOs.RAG
{
    public class IndexDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public string? Keywords { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Modified { get; set; }
    }
}
