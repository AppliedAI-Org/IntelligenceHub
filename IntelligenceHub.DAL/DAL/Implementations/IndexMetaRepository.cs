using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing index metadata in the database.
    /// </summary>
    public class IndexMetaRepository : GenericRepository<DbIndexMetadata>, IIndexMetaRepository
    {
        /// <summary>
        /// Constructor for the IndexMetaRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        public IndexMetaRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves an index metadata entity by its name.
        /// </summary>
        /// <param name="name">The name of the index.</param>
        /// <returns>The index's metadata.</returns>
        public async Task<DbIndexMetadata?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(im => im.Name == name);
        }
    }
}