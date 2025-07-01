using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing profile-tool associations in the database.
    /// </summary>
    public interface IProfileToolsAssociativeRepository
    {
        /// <summary>
        /// Retrieves a list of tool associations for a given profile ID.
        /// </summary>
        /// <param name="profileId">The ID of the profile.</param>
        /// <returns>A list of profile tools.</returns>
        Task<List<DbProfileTool>> GetToolAssociationsAsync(int profileId);

        /// <summary>
        /// Associates a profile with a tool in the associative database.
        /// </summary>
        /// <param name="profileTool">The DbProfiletTool model object.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<DbProfileTool> AddAsync(DbProfileTool profileTool);

        /// <summary>
        /// Associates a list of tools with a profile by their IDs.
        /// </summary>
        /// <param name="profileId">The profile ID.</param>
        /// <param name="toolIds">The list of tool IDs.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> AddAssociationsByProfileIdAsync(int profileId, List<int> toolIds);

        /// <summary>
        /// Associates a list of profiles with a tool by the profile's name.
        /// </summary>
        /// <param name="toolId">The list of tool IDs.</param>
        /// <param name="profileNames">The profile name.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> AddAssociationsByToolIdAsync(int toolId, List<string> profileNames);

        /// <summary>
        /// Deletes all tool associations for a given profile.
        /// </summary>
        /// <param name="profileId">The ID of the profile.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteAllProfileAssociationsAsync(int profileId);

        /// <summary>
        /// Deletes all profile associations for a given tool.
        /// </summary>
        /// <param name="toolId">The ID of the tool.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteAllToolAssociationsAsync(int toolId);

        /// <summary>
        /// Deletes a tool association for a given profile.
        /// </summary>
        /// <param name="toolId">The ID of the tool.</param>
        /// <param name="profileName">The name of the profile.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteToolAssociationAsync(int toolId, string profileName);

        /// <summary>
        /// Deletes a profile association for a given tool.
        /// </summary>
        /// <param name="profileId">The ID of the profile.</param>
        /// <param name="toolName">The name of the tool.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteProfileAssociationAsync(int profileId, string toolName);
    }
}
