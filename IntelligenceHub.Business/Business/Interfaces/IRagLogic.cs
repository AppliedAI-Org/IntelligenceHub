using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.Business.Interfaces
{
    public interface IRagLogic
    {
        Task<IndexMetadata?> GetRagIndex(string index);
        Task<IEnumerable<IndexMetadata>> GetAllIndexesAsync();
        Task<bool> CreateIndex(IndexMetadata indexDefinition);
        Task<bool> ConfigureIndex(IndexMetadata newDefinition);
        Task<bool> DeleteIndex(string index);
        Task<List<IndexDocument>?> QueryIndex(string index, string query);
        Task<bool> RunIndexUpdate(string index);
        Task<IEnumerable<IndexDocument>?> GetAllDocuments(string index, int count, int page);
        Task<IndexDocument?> GetDocument(string index, string document);
        Task<bool> UpsertDocuments(string index, RagUpsertRequest documentUpsertRequest);
        Task<int> DeleteDocuments(string index, string[] documentList);
    }
}
