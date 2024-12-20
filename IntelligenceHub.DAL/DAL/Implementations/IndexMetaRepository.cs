using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    public class IndexMetaRepository : GenericRepository<DbIndexMetadata>, IIndexMetaRepository
    {
        public IndexMetaRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        public async Task<DbIndexMetadata?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(im => im.Name == name);
        }
    }
}