using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing tool properties in the database.
    /// </summary>
    public class PropertyRepository : GenericRepository<DbProperty>, IPropertyRepository
    {
        /// <summary>
        /// Constructor for the PropertyRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        public PropertyRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves all properties associated with a specific tool.
        /// </summary>
        /// <param name="toolId">The ID of the tool.</param>
        /// <returns>A list of tool properties.</returns>
        public async Task<IEnumerable<DbProperty>> GetToolProperties(int toolId)
        {
            return await _context.Properties
                .Where(p => p.ToolId == toolId)
                .ToListAsync();
        }
    }
}