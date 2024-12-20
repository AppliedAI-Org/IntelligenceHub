using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    public class IndexMetaRepository : GenericRepository<DbIndexMetadata>, IIndexMetaRepository
    {
        public IndexMetaRepository(IOptionsMonitor<Settings> settings) : base(settings.CurrentValue.DbConnectionString)
        {
        }

        public async Task<IEnumerable<DbIndexMetadata>> GetAllAsync(int? count = null, int? page = null)
        {
            return await base.GetAllAsync(count, page);
        }

        public async Task<DbIndexMetadata?> GetByNameAsync(string name)
        {
            return await base.GetByNameAsync(name);
        }

        public async Task<DbIndexMetadata> AddAsync(DbIndexMetadata entity)
        {
            return await base.AddAsync(entity);
        }

        public async Task<int> DeleteAsync(DbIndexMetadata entity, string name)
        {
            return await base.DeleteAsync(entity, name);
        }
    }
}