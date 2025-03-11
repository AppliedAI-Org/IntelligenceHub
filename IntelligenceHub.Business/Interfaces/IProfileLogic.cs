using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Business logic for handling profile operations.
    /// </summary>
    public interface IProfileLogic
    {
        /// <summary>
        /// Retrieves a profile by name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>An <see cref="APIResponseWrapper{Profile}"/> containing the profile, if one with the requested name exists, otherwise null.</returns>
        Task<APIResponseWrapper<Profile>> GetProfile(string name);

        /// <summary>
        /// Retrieves all profiles.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of profiles to retrieve.</param>
        /// <returns>An <see cref="APIResponseWrapper{IEnumerable{Profile}}"/> containing a list of all existing profiles.</returns>
        Task<APIResponseWrapper<IEnumerable<Profile>>> GetAllProfiles(int page, int count);

        /// <summary>
        /// Creates or updates an AGI client profile.
        /// </summary>
        /// <param name="profileDto">The request body used to create or update the profile.</param>
        /// <returns>An <see cref="APIResponseWrapper{string}"/> containing an error message if the profile DTO fails to pass validation, otherwise null.</returns>
        Task<APIResponseWrapper<string>> CreateOrUpdateProfile(Profile profileDto);

        /// <summary>
        /// Deletes an AGI client profile by name.
        /// </summary>
        /// <param name="name">The name of the profile to delete.</param>
        /// <returns>An <see cref="APIResponseWrapper{string}"/> containing an error message if the operation fails, otherwise null.</returns>
        Task<APIResponseWrapper<string>> DeleteProfile(string name);

        /// <summary>
        /// Retrieves a tool by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>An <see cref="APIResponseWrapper{Tool}"/> containing the tool if it exists in the database.</returns>
        Task<APIResponseWrapper<Tool>> GetTool(string name);

        /// <summary>
        /// Retrieves all tools in the database.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of tools to retrieve.</param>
        /// <returns>An <see cref="APIResponseWrapper{IEnumerable{Tool}}"/> containing a list of all existing tools.</returns>
        Task<APIResponseWrapper<IEnumerable<Tool>>> GetAllTools(int page, int count);

        /// <summary>
        /// Retrieves the tools associated with a profile.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{string}}"/> containing a list of tool names.</returns>
        Task<APIResponseWrapper<List<string>>> GetProfileToolAssociations(string name);

        /// <summary>
        /// Retrieves the profiles associated with a tool.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{string}}"/> containing a list of profile names.</returns>
        Task<APIResponseWrapper<List<string>>> GetToolProfileAssociations(string name);

        /// <summary>
        /// Creates or updates a list of tools.
        /// </summary>
        /// <param name="toolList">A list of tools to create or update.</param>
        /// <returns>An <see cref="APIResponseWrapper{string}"/> containing an error message if the operation failed, otherwise null.</returns>
        Task<APIResponseWrapper<string>> CreateOrUpdateTools(List<Tool> toolList);

        /// <summary>
        /// Associates a list of profiles with a tool.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <param name="profiles">A list of profile names.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{string}}"/> containing an error message if the operation fails, otherwise null.</returns>
        Task<APIResponseWrapper<List<string>>> AddToolToProfiles(string name, List<string> profiles);

        /// <summary>
        /// Associates a list of tools with a profile.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <param name="tools">A list of tool names.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{string}}"/> containing an error message if the operation fails, otherwise null.</returns>
        Task<APIResponseWrapper<List<string>>> AddProfileToTools(string name, List<string> tools);

        /// <summary>
        /// Deletes the associations between a tool and a list of profiles.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <param name="profiles">A list of profile names.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{string}}"/> containing an error message if the operation fails, otherwise null.</returns>
        Task<APIResponseWrapper<List<string>>> DeleteToolAssociations(string name, List<string> profiles);

        /// <summary>
        /// Deletes the associations between a profile and a list of tools.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <param name="tools">A list of tool names.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{string}}"/> containing an error message if the operation fails, otherwise null.</returns>
        Task<APIResponseWrapper<List<string>>> DeleteProfileAssociations(string name, List<string> tools);

        /// <summary>
        /// Deletes a tool by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating the success of the operation.</returns>
        Task<APIResponseWrapper<bool>> DeleteTool(string name);

        /// <summary>
        /// Adds or updates the properties associated with a tool.
        /// </summary>
        /// <param name="existingTool">The existing tool.</param>
        /// <param name="newProperties">A dictionary of properties where the name of the 
        /// property is the key, and the value is the property object.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating the success of the operation.</returns>
        Task<APIResponseWrapper<bool>> AddOrUpdateToolProperties(Tool existingTool, Dictionary<string, Property> newProperties);
    }
}
