using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing index documents in the database. This repository is unique in that its table name is dynamic, and must be provided for each request.
    /// </summary>
    public class IndexRepository : GenericRepository<DbIndexDocument>, IIndexRepository
    {
        /// <summary>
        /// Constructor for the IndexRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        public IndexRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves the length of the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the RAG index.</param>
        /// <returns>The length of the index.</returns>
        public async Task<int> GetRagIndexLengthAsync(string tableName)
        {
            var query = $@"SELECT COUNT(*) FROM [{tableName}]";
            return await _context.Database.ExecuteSqlRawAsync(query);
        }

        /// <summary>
        /// Retrieves a document from the RAG index by its title.
        /// </summary>
        /// <param name="tableName">The name of the RAG index.</param>
        /// <param name="title">The name of the document.</param>
        /// <returns>The document, or null if no matching entry is found.</returns>
        public async Task<DbIndexDocument?> GetDocumentAsync(string tableName, string title)
        {
            var query = $@"SELECT * FROM [{tableName}] WHERE Title = @Title";
            var parameters = new[] { new SqlParameter("@Title", title) };
            return await _dbSet.FromSqlRaw(query, parameters).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new table for RAG indexing.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success or failure of the operation.</returns>
        public async Task<bool> CreateIndexAsync(string tableName)
        {
            var query = $@"CREATE TABLE [{tableName}] (
                                Id INT IDENTITY(1,1) PRIMARY KEY,
                                Title NVARCHAR(255) NOT NULL,
                                Content NVARCHAR(MAX) NOT NULL,
                                Topic NVARCHAR(255),
                                Keywords NVARCHAR(255),
                                Source NVARCHAR(4000) NOT NULL,
                                Created DATETIMEOFFSET NOT NULL,
                                Modified DATETIMEOFFSET NOT NULL
                            );";

            await _context.Database.ExecuteSqlRawAsync(query);
            return true;
        }

        /// <summary>
        /// Enables change tracking for the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> EnableChangeTrackingAsync(string tableName)
        {
            var query = $@"ALTER TABLE [dbo].[{tableName}]
                           ENABLE CHANGE_TRACKING 
                           WITH (TRACK_COLUMNS_UPDATED = ON);";

            await _context.Database.ExecuteSqlRawAsync(query);
            return true;
        }

        /// <summary>
        /// Marks the index to be updated by an indexer by setting its state to modified.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> MarkIndexForUpdateAsync(string tableName)
        {
            // Dummy operation to mark the whole index as updated
            var query = $@"UPDATE [{tableName}] SET Modified = Modified";
            await _context.Database.ExecuteSqlRawAsync(query);
            return true;
        }

        /// <summary>
        /// Deletes the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteIndexAsync(string tableName)
        {
            var query = $@"DROP TABLE IF EXISTS [{tableName}]";
            var result = await _context.Database.ExecuteSqlRawAsync(query);
            return result == -1; // -1 is returned for scenarios where row number doesn't make sense
        }

        /// <summary>
        /// Retrieves all documents from the RAG index.
        /// </summary>
        /// <param name="tableName">The name of the index.</param>
        /// <param name="count">The number of documents to retrieve.</param>
        /// <param name="page">The page number to offset the results by.</param>
        /// <returns>An IEnumerable containing the documents.</returns>
        public async Task<IEnumerable<DbIndexDocument>> GetAllAsync(string tableName, int count, int page)
        {
            // Calculate the number of rows to skip based on the page number
            var skip = (page - 1) * count;

            // Formulate the SQL query for pagination
            var query = $@"SELECT * FROM [{tableName}]
                           ORDER BY Id
                           OFFSET {skip} ROWS
                           FETCH NEXT {count} ROWS ONLY";

            // Execute the query and return the result
            var responseCollection = await _dbSet.FromSqlRaw(query).ToListAsync();
            return responseCollection;
        }

        /// <summary>
        /// Adds a new document to the RAG index.
        /// </summary>
        /// <param name="document">The document being added.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>The newly added document.</returns>
        public async Task<DbIndexDocument> AddAsync(DbIndexDocument document, string tableName)
        {
            var query = $@"INSERT INTO [{tableName}] (Title, Content, Topic, Keywords, Source, Created, Modified)
                               VALUES (@Title, @Content, @Topic, @Keywords, @Source, @Created, @Modified);
                               SELECT CAST(SCOPE_IDENTITY() as int);";
            var parameters = new[]
            {
                    new SqlParameter("@Title", document.Title),
                    new SqlParameter("@Content", document.Content),
                    new SqlParameter("@Topic", document.Topic ?? (object)DBNull.Value),
                    new SqlParameter("@Keywords", document.Keywords ?? (object)DBNull.Value),
                    new SqlParameter("@Source", document.Source),
                    new SqlParameter("@Created", document.Created),
                    new SqlParameter("@Modified", document.Modified)
                };
            document.Id = await _context.Database.ExecuteSqlRawAsync(query, parameters);
            return document;
        }

        /// <summary>
        /// Updates an existing document in the RAG index.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <param name="document">The new definition of the document.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> UpdateAsync(int id, DbIndexDocument document, string tableName)
        {
            var query = $@"UPDATE [{tableName}] SET 
                               Title = @Title, 
                               Content = @Content, 
                               Topic = @Topic, 
                               Keywords = @Keywords, 
                               Source = @Source, 
                               Created = @Created, 
                               Modified = @Modified 
                               WHERE Id = @Id";
            var parameters = new[]
            {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@Title", document.Title),
                    new SqlParameter("@Content", document.Content),
                    new SqlParameter("@Topic", document.Topic ?? (object)DBNull.Value),
                    new SqlParameter("@Keywords", document.Keywords ?? (object)DBNull.Value),
                    new SqlParameter("@Source", document.Source),
                    new SqlParameter("@Created", document.Created),
                    new SqlParameter("@Modified", document.Modified)
                };
            return await _context.Database.ExecuteSqlRawAsync(query, parameters) == 1;
        }

        /// <summary>
        /// Deletes a document from the RAG index.
        /// </summary>
        /// <param name="document">The document to be deleted.</param>
        /// <param name="tableName">The name of the index.</param>
        /// <returns>A bool indicating the success or failure of the operation.</returns>
        public async Task<bool> DeleteAsync(DbIndexDocument document, string tableName)
        {
            var query = $@"DELETE FROM [{tableName}] WHERE Id = @Id";
            var parameters = new[]
            {
                    new SqlParameter("@Id", document.Id)
                };
            return await _context.Database.ExecuteSqlRawAsync(query, parameters) == 1;
        }
    }
}