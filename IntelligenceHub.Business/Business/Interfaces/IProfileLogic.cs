using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Business logic for handling profile operations
    /// </summary>
    public interface IProfileLogic
    {
        /// <summary>
        /// Retrieves a profile by name
        /// </summary>
        /// <param name="name">The name of the profile</param>
        /// <returns>The profile, if one with the requested name exists, otherwise null</returns>
        Task<Profile?> GetProfile(string name);

        /// <summary>
        /// Retrieves all profiles
        /// </summary>
        /// <returns>An list of all existing profiles</returns>
        Task<IEnumerable<Profile>> GetAllProfiles();

        /// <summary>
        /// Creates or updates an AGI client profile
        /// </summary>
        /// <param name="profileDto">The request body used to create or update the profile</param>
        /// <returns>An error message if the profile DTO fails to pass validation, otherwise null</returns>
        Task<string?> CreateOrUpdateProfile(Profile profileDto);

        /// <summary>
        /// Deletes an AGI client profile by name
        /// </summary>
        /// <param name="name">The name of the profile to delete</param>
        /// <returns>An error message if the operation faile, otherwise null</returns>
        Task<string?> DeleteProfile(string name);

        /// <summary>
        /// Retrieves a tool by name
        /// </summary>
        /// <param name="name">The name of the tool</param>
        /// <returns>The tool if it exists in the database</returns>
        Task<Tool?> GetTool(string name);

        /// <summary>
        /// Retrieves all tools in the database
        /// </summary>
        /// <returns>A list of all existing tools</returns>
        Task<IEnumerable<Tool>> GetAllTools();

        /// <summary>
        /// Retrieves the tools associated with a profile
        /// </summary>
        /// <param name="name">The name of the profile</param>
        /// <returns>A list of tool names</returns>
        Task<List<string>> GetProfileToolAssociations(string name);

        /// <summary>
        /// Retrieves the profiles associated with a tool
        /// </summary>
        /// <param name="name">The name of the tools</param>
        /// <returns>A list of profile names</returns>
        Task<List<string>> GetToolProfileAssociations(string name);

        /// <summary>
        /// Creates or updates a list of tools
        /// </summary>
        /// <param name="toolList">A list of tools to create or update</param>
        /// <returns>An error message if the operation failed, otherwise null</returns>
        Task<string?> CreateOrUpdateTools(List<Tool> toolList);

        /// <summary>
        /// Associates a list of profiles with a tool
        /// </summary>
        /// <param name="name">The name of the tool</param>
        /// <param name="profiles">A list of profile names</param>
        /// <returns>An error message if the operation fails, otherwise null</returns>
        Task<string?> AddToolToProfiles(string name, List<string> profiles);

        /// <summary>
        /// Associates a list of tools with a profile
        /// </summary>
        /// <param name="name">The name of the profile</param>
        /// <param name="tools">A list of tool names</param>
        /// <returns>An error message if the operation fails, otherwise null</returns>
        Task<string?> AddProfileToTools(string name, List<string> tools);

        /// <summary>
        /// Deletes the associations between a tool and a list of profiles
        /// </summary>
        /// <param name="name">The name of the tool</param>
        /// <param name="profiles">A list of profile names</param>
        /// <returns>An error message if the operation fails, otherwise null</returns>
        Task<string?> DeleteToolAssociations(string name, List<string> profiles);

        /// <summary>
        /// Deletes the associations between a profile and a list of tools
        /// </summary>
        /// <param name="name">The name of the profile</param>
        /// <param name="tools">A list of tool names</param>
        /// <returns>An error message if the operation fails, otherwise null</returns>
        Task<string?> DeleteProfileAssociations(string name, List<string> tools);

        /// <summary>
        /// Deletes a tool by name
        /// </summary>
        /// <param name="name">The name of the tool</param>
        /// <returns>A bool indicating the success of the operation</returns>
        Task<bool> DeleteTool(string name);

        /// <summary>
        /// Adds or updates the properties associated with a tool
        /// </summary>
        /// <param name="existingTool">The existing tool</param>
        /// <param name="newProperties">A dictionary of properties where the name of the 
        /// property is the key, and the value is the property object</param>
        /// <returns>A bool indicating the success of the operation</returns>
        Task<bool> AddOrUpdateToolProperties(Tool existingTool, Dictionary<string, Property> newProperties);
    }
}
