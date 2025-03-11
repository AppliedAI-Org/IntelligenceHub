using IntelligenceHub.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Generic repository for CRUD operations on entities of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : class, new()
    {
        protected readonly IntelligenceHubDbContext _context;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// Constructor for the GenericRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        public GenericRepository(IntelligenceHubDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // Prodive a methodology for safely exposing database
        public DatabaseFacade Database => _context.Database;

        /// <summary>
        /// Retrieves all entities of type T from the database.
        /// </summary>
        /// <param name="count">The number of entities to retrieve.</param>
        /// <param name="page">The page used to offset the collection.</param>
        /// <returns>A collection of entities of type T.</returns>
        public async Task<IEnumerable<T>> GetAllAsync(int? count = null, int? page = null)
        {
            var query = _dbSet.AsQueryable();
            if (count.HasValue && page.HasValue && count > 0 && page > 0)
            {
                query = query.Skip((page.Value - 1) * count.Value).Take(count.Value);
            }
            return await query.ToListAsync();
        }

        /// <summary>
        /// Adds a new entity of type T to the database.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The successfully added entity.</returns>
        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            await _context.Entry(entity).ReloadAsync();
            return entity;
        }

        /// <summary>
        /// Updates an existing entity of type T in the database.
        /// </summary>
        /// <param name="entity">The updated entity.</param>
        /// <returns>The updated entity.</returns>
        public async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await _context.Entry(entity).ReloadAsync();
            return entity;
        }


        /// <summary>
        /// Deletes an entity of type T from the database.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        public async Task<bool> DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync() > 0; // ensure an entity was deleted
        }
    }
}