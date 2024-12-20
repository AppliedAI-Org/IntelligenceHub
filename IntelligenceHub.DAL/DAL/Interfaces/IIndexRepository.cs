using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IIndexRepository
    {
        Task<int> GetRagIndexLengthAsync(string tableName);
        Task<DbIndexDocument?> GetDocumentAsync(string tableName, string title);
        Task<bool> CreateIndexAsync(string tableName);
        Task<bool> DeleteIndexAsync(string tableName);
        Task<IEnumerable<DbIndexDocument>> GetAllAsync(int count, int page);
        Task<DbIndexDocument> AddAsync(DbIndexDocument document, string tableName);
        Task<int> UpdateAsync(DbIndexDocument existing, DbIndexDocument document, string tableName);
        Task<int> DeleteAsync(DbIndexDocument document, string tableName);
    }
}