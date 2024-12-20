using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IToolRepository
    {
        Task<DbTool?> GetByIdAsync(int id);
        Task<List<string>> GetProfileToolsAsync(string name);
        Task<List<string>> GetToolProfilesAsync(string name);
        Task<DbTool?> GetByNameAsync(string name);
        Task<IEnumerable<DbTool>> GetAllAsync(int? count = null, int? page = null);
        Task<int> UpdateAsync(DbTool existingTool, DbTool updateToolDto);
        Task<DbTool> AddAsync(DbTool tool);
        Task<int> DeleteAsync(DbTool tool);
    }
}
