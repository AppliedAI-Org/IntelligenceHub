using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing index documents in the database. This repository is unique in that its table name is dynamic, and must be provided for each request.
    /// </summary>
    public interface IIndexRepository
    {
        /// <summary>
        /// Retrieves the length of the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the RAG index.</param>
        /// <returns>The length of the index.</returns>
        Task<int> GetRagIndexLengthAsync(string tableName);

        /// <summary>
        /// Retrieves a document from the RAG index by its title.
        /// </summary>
        /// <param name="tableName">The name of the RAG index.</param>
        /// <param name="title">The name of the document.</param>
        /// <returns>The document, or null if no matching entry is found.</returns>
        Task<DbIndexDocument?> GetDocumentAsync(string tableName, string title);

        /// <summary>
        /// Creates a new table for RAG indexing.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success or failure of the operation.</returns>
        Task<bool> CreateIndexAsync(string tableName);

        /// <summary>
        /// Enables change tracking for the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> EnableChangeTrackingAsync(string tableName);

        /// <summary>
        /// Marks the index to be updated by an indexer by setting its state to modified.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> MarkIndexForUpdateAsync(string tableName);

        /// <summary>
        /// Deletes the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteIndexAsync(string tableName);

        /// <summary>
        /// Retrieves all documents from the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <param name="count">The number of documents to retrieve.</param>
        /// <param name="page">The page number to offset the results by.</param>
        /// <returns>An IEnumerable containing the documents.</returns>
        Task<IEnumerable<DbIndexDocument>> GetAllAsync(string tableName, int count, int page);

        /// <summary>
        /// Adds a new document to the RAG index.
        /// </summary>
        /// <param name="document">The document being added.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>The newly added document.</returns>
        Task<DbIndexDocument> AddAsync(DbIndexDocument document, string tableName);

        /// <summary>
        /// Updates an existing document in the RAG index.
        /// </summary>
        /// <param name="existing">The current definition of the document.</param>
        /// <param name="document">The new definition of the document.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> UpdateAsync(DbIndexDocument existing, DbIndexDocument document, string tableName);

        /// <summary>
        /// Deletes a document from the RAG index.
        /// </summary>
        /// <param name="document">The document to be deleted.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns></returns>
        Task<int> DeleteAsync(DbIndexDocument document, string tableName);
    }
}