using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    public class ToolRepository : GenericRepository<DbTool>, IToolRepository
    {
        public ToolRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        public async Task<DbTool?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<DbTool?> GetByIdAsync(int id)
        {
            return await _context.Tools.FirstOrDefaultAsync(t => t.Id == id);
        }

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