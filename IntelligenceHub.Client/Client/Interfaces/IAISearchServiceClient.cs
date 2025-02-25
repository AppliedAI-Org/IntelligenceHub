using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.Client.Interfaces
{
    public interface IAISearchServiceClient
    {
        public Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query);
        public Task<bool> UpsertIndex(IndexMetadata indexDefinition);
        public Task<bool> DeleteIndex(string indexName);
        public Task<bool> UpsertIndexer(IndexMetadata index);
        public Task<bool> RunIndexer(string indexName);
        public Task<bool> DeleteIndexer(string indexName, string embeddingModel);
        public Task<bool> CreateDatasource(string databaseName);
        public Task<bool> DeleteDatasource(string indexName);
    }
}
