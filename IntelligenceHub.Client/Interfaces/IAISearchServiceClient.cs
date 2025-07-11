using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.Client.Interfaces
{
    /// <summary>
    /// A Azure AI Search Services client oriented around RAG construction and consumption.
    /// </summary>
    public interface IAISearchServiceClient
    {
        /// <summary>
        /// Retrieves the metadata for a RAG index.
        /// </summary>
        /// <param name="index">The definition of the RAG index.</param>
        /// <param name="query">The query to search against the RAG index.</param>
        /// <returns>Returns the search results retrieved from the RAG index.</returns>
        public Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query);

        /// <summary>
        /// Creates or updates a RAG index.
        /// </summary>
        /// <param name="indexDefinition">The definition of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public Task<bool> UpsertIndex(IndexMetadata indexDefinition);

        /// <summary>
        /// Deletes a RAG index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public Task<bool> DeleteIndex(string indexName);

        /// <summary>
        /// Creates or updates a RAG indexer.
        /// </summary>
        /// <param name="index">The new definition of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public Task<bool> UpsertIndexer(IndexMetadata index);

        /// <summary>
        /// Updates the data in a RAG index, syncing with the corresponding SQL table.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public Task<bool> RunIndexer(string indexName);

        /// <summary>
        /// Deletes a RAG indexer and the associated skillset.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public Task<bool> DeleteIndexer(string indexName);

        /// <summary>
        /// Creates a datasource connection to the SQL database table created for this index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public Task<bool> CreateDatasource(string databaseName);

        /// <summary>
        /// Deletes a datasource connection to the SQL database table created for this index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public Task<bool> DeleteDatasource(string indexName);
    }
}
