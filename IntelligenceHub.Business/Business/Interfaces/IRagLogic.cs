using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Business logic for handling RAG operations.
    /// </summary>
    public interface IRagLogic
    {
        /// <summary>
        /// Retrieves the metadata for a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <returns>An <see cref="APIResponseWrapper{IndexMetadata}"/> containing the index definition, if one exists.</returns>
        Task<APIResponseWrapper<IndexMetadata>> GetRagIndex(string index);

        /// <summary>
        /// Retrieves all RAG metadata associated with RAG indexes.
        /// </summary>
        /// <returns>An <see cref="APIResponseWrapper{IEnumerable{IndexMetadata}}"/> containing a list of index metadata.</returns>
        Task<APIResponseWrapper<IEnumerable<IndexMetadata>>> GetAllIndexesAsync();

        /// <summary>
        /// Creates a new RAG index.
        /// </summary>
        /// <param name="indexDefinition">The definition of the index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        Task<APIResponseWrapper<bool>> CreateIndex(IndexMetadata indexDefinition);

        /// <summary>
        /// Configures an existing RAG index.
        /// </summary>
        /// <param name="indexDefinition">The new definition of the index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        Task<APIResponseWrapper<bool>> ConfigureIndex(IndexMetadata newDefinition);

        /// <summary>
        /// Deletes a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        Task<APIResponseWrapper<bool>> DeleteIndex(string index);

        /// <summary>
        /// Queries a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="query">The query to search against the RAG index.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{IndexDocument}}"/> containing a list of documents most closely matching the query.</returns>
        Task<APIResponseWrapper<List<IndexDocument>>> QueryIndex(string index, string query);

        /// <summary>
        /// Runs an update on a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        Task<APIResponseWrapper<bool>> RunIndexUpdate(string index);

        /// <summary>
        /// Retrieves all documents from a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="count">The number of documents to retrieve.</param>
        /// <param name="page">The current page number.</param>
        /// <returns>An <see cref="APIResponseWrapper{IEnumerable{IndexDocument}}"/> containing a list of documents in the RAG index.</returns>
        Task<APIResponseWrapper<IEnumerable<IndexDocument>>> GetAllDocuments(string index, int count, int page);

        /// <summary>
        /// Retrieves a single document from a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="document">The title/name of the document.</param>
        /// <returns>An <see cref="APIResponseWrapper{IndexDocument}"/> containing the matching document, or null if none exists.</returns>
        Task<APIResponseWrapper<IndexDocument>> GetDocument(string index, string document);

        /// <summary>
        /// Upserts documents into a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="documentUpsertRequest">The request body containing the documents to upsert.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        Task<APIResponseWrapper<bool>> UpsertDocuments(string index, RagUpsertRequest documentUpsertRequest);

        /// <summary>
        /// Deletes documents from a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="documentList">A list of document titles/names to delete.</param>
        /// <returns>An <see cref="APIResponseWrapper{int}"/> indicating the number of documents that were successfully deleted.</returns>
        Task<APIResponseWrapper<int>> DeleteDocuments(string index, string[] documentList);
    }
}
