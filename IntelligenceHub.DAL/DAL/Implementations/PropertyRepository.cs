using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    public class PropertyRepository : GenericRepository<DbProperty>, IPropertyRepository
    {
        public PropertyRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DbProperty>> GetToolProperties(int toolId)
        {
            return await _context.Properties
                .Where(p => p.ToolId == toolId)
                .ToListAsync();
        }

        public async Task<DbProperty> UpdatePropertyAsync(DbProperty existingEntity, DbProperty entity)
        {
            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();
            return existingEntity;
        }
    }
}