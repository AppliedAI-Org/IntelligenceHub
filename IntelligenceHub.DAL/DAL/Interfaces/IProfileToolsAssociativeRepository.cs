using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IProfileToolsAssociativeRepository
    {
        Task<List<DbProfileTool>> GetToolAssociationsAsync(int profileId);
        Task<bool> AddAssociationsByProfileIdAsync(int profileId, List<int> toolIds);
        Task<bool> AddAssociationsByToolIdAsync(int toolId, List<string> profileNames);
        Task<int> DeleteAllProfileAssociationsAsync(int profileId);
        Task<int> DeleteAllToolAssociationsAsync(int toolId);
        Task<int> DeleteToolAssociationAsync(int toolId, string profileName);
        Task<int> DeleteProfileAssociationAsync(int profileId, string toolName);
    }
}
