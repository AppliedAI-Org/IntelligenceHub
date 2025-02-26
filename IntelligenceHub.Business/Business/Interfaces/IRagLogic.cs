using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Business logic for handling RAG operations
    /// </summary>
    public interface IRagLogic
    {
        /// <summary>
        /// Retrieves the metadata for a RAG index
        /// </summary>
        /// <param name="index">The name of the index</param>
        /// <returns>The index definition, if one exists</returns>
        Task<IndexMetadata?> GetRagIndex(string index);

        /// <summary>
        /// Retrieves all RAG metadata associated with RAG indexes
        /// </summary>
        /// <returns>A list of index metadata</returns>
        Task<IEnumerable<IndexMetadata>> GetAllIndexesAsync();

        /// <summary>
        /// Creates a new RAG index
        /// </summary>
        /// <param name="indexDefinition">The definition of the index</param>
        /// <returns>A boolean indicating success or failure</returns>
        Task<bool> CreateIndex(IndexMetadata indexDefinition);

        /// <summary>
        /// Configures an existing RAG index
        /// </summary>
        /// <param name="indexDefinition">The new definition of the index</param>
        /// <returns>A boolean indicating success or failure</returns>
        Task<bool> ConfigureIndex(IndexMetadata newDefinition);

        /// <summary>
        /// Deletes a RAG index
        /// </summary>
        /// <param name="index">The name of the RAG index</param>
        /// <returns>A boolean indicating success or failure</returns>
        Task<bool> DeleteIndex(string index);

        /// <summary>
        /// Queries a RAG index
        /// </summary>
        /// <param name="index">The name of the RAG index</param>
        /// <param name="query">The query to search against the RAG index</param>
        /// <returns>A list of documents most closely matching the query</returns>
        Task<List<IndexDocument>?> QueryIndex(string index, string query);

        /// <summary>
        /// Runs an update on a RAG index
        /// </summary>
        /// <param name="index">The name of the RAG index</param>
        /// <returns>A bool indicating success or failure</returns>
        Task<bool> RunIndexUpdate(string index);

        /// <summary>
        /// Retrieves all documents from a RAG index
        /// </summary>
        /// <param name="index">The name of the RAG index</param>
        /// <param name="count">The number of documents to retreive</param>
        /// <param name="page">The current page number</param>
        /// <returns>A list of documents in the RAG index</returns>
        Task<IEnumerable<IndexDocument>?> GetAllDocuments(string index, int count, int page);

        /// <summary>
        /// Retrieves a single document from a RAG index
        /// </summary>
        /// <param name="index">The name of the RAG index</param>
        /// <param name="document">The title/name of the document</param>
        /// <returns>A matching document, or null if none exists</returns>
        Task<IndexDocument?> GetDocument(string index, string document);

        /// <summary>
        /// Upserts documents into a RAG index
        /// </summary>
        /// <param name="index">The name of the index</param>
        /// <param name="documentUpsertRequest">The request body containing the documents to upsert</param>
        /// <returns>A boolean indicating success or failure</returns>
        Task<bool> UpsertDocuments(string index, RagUpsertRequest documentUpsertRequest);

        /// <summary>
        /// Deletes documents from a RAG index
        /// </summary>
        /// <param name="index">The name of the RAG index</param>
        /// <param name="documentList">A list of document titles/names to delete</param>
        /// <returns>In integer indicating the number of documents that were succesfully deleted</returns>
        Task<int> DeleteDocuments(string index, string[] documentList);
    }
}
