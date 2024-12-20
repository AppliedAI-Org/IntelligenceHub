using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;

namespace IntelligenceHub.Business.Interfaces
{
    public interface IProfileLogic
    {
        Task<Profile?> GetProfile(string name);
        Task<IEnumerable<Profile>> GetAllProfiles();
        Task<string?> CreateOrUpdateProfile(Profile profileDto);
        Task<string> DeleteProfile(string name);
        Task<Tool> GetTool(string name);
        Task<IEnumerable<Tool>> GetAllTools();
        Task<List<string>> GetProfileToolAssociations(string name);
        Task<List<string>> GetToolProfileAssociations(string name);
        Task<string> CreateOrUpdateTools(List<Tool> toolList);
        Task<string> AddToolToProfiles(string name, List<string> profiles);
        Task<string> AddProfileToTools(string name, List<string> tools);
        Task<string> DeleteToolAssociations(string name, List<string> profiles);
        Task<string> DeleteProfileAssociations(string name, List<string> tools);
        Task<bool> DeleteTool(string name);
        Task<bool> AddOrUpdateToolProperties(Tool existingTool, Dictionary<string, Property> newProperties);
    }
}
