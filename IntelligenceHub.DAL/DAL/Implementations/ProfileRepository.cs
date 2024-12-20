using IntelligenceHub.API.DTOs;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.DAL.Implementations
{
    public class ProfileRepository : GenericRepository<DbProfile>, IProfileRepository
    {
        public ProfileRepository(IntelligenceHubDbContext context) : base(context)
        {
        }

        public async Task<DbProfile?> GetByNameAsync(string name)
        {
            return await _dbSet
                .Include(p => p.ProfileTools)
                .ThenInclude(pt => pt.Tool)
                .FirstOrDefaultAsync(p => p.Name == name);
        }
    }
}