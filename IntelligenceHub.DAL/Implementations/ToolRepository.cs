using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    /// <summary>
    /// Repository for managing tools in the database.
    /// </summary>
    public class ToolRepository : GenericRepository<DbTool>, IToolRepository
    {
        /// <summary>
        /// Constructor for the ToolRepository class.
        /// </summary>
        /// <param name="context">The database context used to map to the SQL database.</param>
        public ToolRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves a tool from the database by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>The tool, or null if none is found.</returns>
        public async Task<DbTool?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name == name);
        }

        /// <summary>
        /// Retrieves a tool from the database by ID.
        /// </summary>
        /// <param name="id">The ID of the tool.</param>
        /// <returns>The tool, or null if none is found.</returns>
        public async Task<DbTool?> GetByIdAsync(int id)
        {
            return await _context.Tools.FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Retrieves a list of tools associated with a given profile name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>A list of tool names.</returns>
        public async Task<List<string>> GetProfileToolsAsync(string name)
        {
            var profileTools = await _context.ProfileTools
                .Include(pt => pt.Tool)
                .Include(pt => pt.Profile)
                .Where(pt => pt.Profile.Name == name)
                .Select(pt => pt.Tool.Name)
                .ToListAsync();

            return profileTools;
        }

        /// <summary>
        /// Retrieves a list of profiles associated with a given tool name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>A list of profile names.</returns>
        public async Task<List<string>> GetToolProfilesAsync(string name)
        {
            var toolProfiles = await _context.ProfileTools
                .Include(pt => pt.Tool)
                .Include(pt => pt.Profile)
                .Where(pt => pt.Tool.Name == name)
                .Select(pt => pt.Profile.Name)
                .ToListAsync();

            return toolProfiles;
        }
    }
}