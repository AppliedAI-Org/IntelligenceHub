using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IToolRepository
    {
        Task<Tool> GetToolByNameAsync(string name);
        Task<Tool> GetToolByIdAsync(int id);
        Task<List<string>> GetProfileToolsAsync(string name);
        Task<List<string>> GetToolProfilesAsync(string name);
        Task<DbTool> GetByNameAsync(string name);
        Task<IEnumerable<DbTool>> GetAllAsync();
        Task UpdateAsync(DbTool existingTool, DbTool updateToolDto);
        Task<DbTool?> AddAsync(DbTool tool);
        Task<int> DeleteAsync(DbTool tool);
    }
}
