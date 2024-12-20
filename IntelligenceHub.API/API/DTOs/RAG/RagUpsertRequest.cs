namespace IntelligenceHub.API.DTOs.RAG
{
    public class RagUpsertRequest
    {
        public List<IndexDocument> Documents { get; set; } = new List<IndexDocument>();
    }
}
