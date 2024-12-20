using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntelligenceHub.DAL.Implementations
{
    // Technically this repository shoul
    public class ProfileToolsAssociativeRepository : GenericRepository<DbProfileTool>, IProfileToolsAssociativeRepository
    {
        public ProfileToolsAssociativeRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        public async Task<List<DbProfileTool>> GetToolAssociationsAsync(int profileId)
        {
            return await _context.ProfileTools
                .Where(pt => pt.ProfileID == profileId)
                .ToListAsync();
        }

        public async Task<bool> AddAssociationsByProfileIdAsync(int profileId, List<int> toolIds)
        {
            foreach (var toolId in toolIds)
            {
                if (!await _context.ProfileTools.AnyAsync(pt => pt.ProfileID == profileId && pt.ToolID == toolId))
                {
                    _context.ProfileTools.Add(new DbProfileTool { ProfileID = profileId, ToolID = toolId });
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddAssociationsByToolIdAsync(int toolId, List<string> profileNames)
        {
            foreach (var name in profileNames)
            {
                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Name == name);
                if (profile != null && !await _context.ProfileTools.AnyAsync(pt => pt.ProfileID == profile.Id && pt.ToolID == toolId))
                {
                    _context.ProfileTools.Add(new DbProfileTool { ProfileID = profile.Id, ToolID = toolId });
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteToolAssociationAsync(int toolId, string profileName)
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Name == profileName);
            if (profile != null)
            {
                var association = await _context.ProfileTools.FirstOrDefaultAsync(pt => pt.ToolID == toolId && pt.ProfileID == profile.Id);
                if (association != null)
                {
                    _context.ProfileTools.Remove(association);
                    return await _context.SaveChangesAsync();
                }
            }
            return 0;
        }

        public async Task<int> DeleteProfileAssociationAsync(int profileId, string toolName)
        {
            var tool = await _context.Tools.FirstOrDefaultAsync(t => t.Name == toolName);
            if (tool != null)
            {
                var association = await _context.ProfileTools.FirstOrDefaultAsync(pt => pt.ProfileID == profileId && pt.ToolID == tool.Id);
                if (association != null)
                {
                    _context.ProfileTools.Remove(association);
                    return await _context.SaveChangesAsync();
                }
            }
            return 0;
        }

        public async Task<int> DeleteAllProfileAssociationsAsync(int profileId)
        {
            var associations = _context.ProfileTools.Where(pt => pt.ProfileID == profileId);
            _context.ProfileTools.RemoveRange(associations);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteAllToolAssociationsAsync(int toolId)
        {
            var associations = _context.ProfileTools.Where(pt => pt.ToolID == toolId);
            _context.ProfileTools.RemoveRange(associations);
            return await _context.SaveChangesAsync();
        }
    }
}