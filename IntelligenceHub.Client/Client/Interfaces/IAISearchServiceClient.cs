using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.Client.Interfaces
{
    public interface IAISearchServiceClient
    {
        public Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query);
        public Task<bool> CreateIndex(IndexMetadata indexDefinition);
        public Task<bool> DeleteIndex(string indexName);
        public Task<bool> CreateIndexer(IndexMetadata index);
        public Task<bool> DeleteIndexer(string indexName, string embeddingModel);
        public Task<bool> CreateDatasource(string databaseName);
        public Task<bool> DeleteDatasource(string indexName);
    }
}
