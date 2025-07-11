namespace IntelligenceHub.API.DTOs.RAG
{
    /// <summary>
    /// Request body used to upsert documents into a RAG index.
    /// </summary>
    public class RagUpsertRequest
    {
        /// <summary>
        /// Gets or sets the documents that will be inserted or updated.
        /// </summary>
        public List<IndexDocument> Documents { get; set; } = new List<IndexDocument>();
    }
}

