using IntelligenceHub.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IntelligenceHub.DAL.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class, new()
    {
        protected readonly IntelligenceHubDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(IntelligenceHubDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // Prodive a methodology for safely exposing database
        public DatabaseFacade Database => _context.Database;

        public async Task<IEnumerable<T>> GetAllAsync(int? count = null, int? page = null)
        {
            var query = _dbSet.AsQueryable();
            if (count.HasValue && page.HasValue && count > 0 && page > 0)
            {
                query = query.Skip((page.Value - 1) * count.Value).Take(count.Value);
            }
            return await query.ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            try
            {
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        // Refactor this after removing 'existingEntity' parameter
        public async Task<int> UpdateAsync(T existingEntity, T entity)
        {
            // Get the entry for the existing entity
            var entry = _context.Entry(existingEntity);

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    continue; // Skip primary key properties
                }

                var newValue = property.Metadata.PropertyInfo?.GetValue(entity);
                property.CurrentValue = newValue;
            }

            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync();
        }
    }
}