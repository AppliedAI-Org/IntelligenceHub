using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IIndexMetaRepository
    {
        Task<DbIndexMetadata?> GetByNameAsync(string name);
        Task<DbIndexMetadata> AddAsync(DbIndexMetadata entity);
        Task<IEnumerable<DbIndexMetadata>> GetAllAsync(int? count = null, int? page = null);
        Task<int> DeleteAsync(DbIndexMetadata entity);
    }
}
