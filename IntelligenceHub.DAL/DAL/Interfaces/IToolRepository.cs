using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing tools in the database.
    /// </summary>
    public interface IToolRepository
    {
        /// <summary>
        /// Retrieves a tool from the database by ID.
        /// </summary>
        /// <param name="id">The ID of the tool.</param>
        /// <returns>The tool, or null if none is found.</returns>
        Task<DbTool?> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves a list of tools associated with a given profile name.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>A list of tool names.</returns>
        Task<List<string>> GetProfileToolsAsync(string name);

        /// <summary>
        /// Retrieves a list of profiles associated with a given tool name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>A list of profile names.</returns>
        Task<List<string>> GetToolProfilesAsync(string name);

        /// <summary>
        /// Retrieves a tool from the database by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>The tool, or null if none is found.</returns>
        Task<DbTool?> GetByNameAsync(string name);

        /// <summary>
        /// Retrieves a list of tools from the database.
        /// </summary>
        /// <param name="count">The number of entities to retrieve.</param>
        /// <param name="page">The page number to offset the results by.</param>
        /// <returns>An IEnumerable of tools.</returns>
        Task<IEnumerable<DbTool>> GetAllAsync(int? count = null, int? page = null);

        /// <summary>
        /// Updates an existing tool in the database.
        /// </summary>
        /// <param name="existingTool">The current definition of the tool.</param>
        /// <param name="updateToolDto">The new definition of the tool.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> UpdateAsync(DbTool existingTool, DbTool updateToolDto);

        /// <summary>
        /// Adds a new tool to the database.
        /// </summary>
        /// <param name="tool">The new tool.</param>
        /// <returns>The newly created tool.</returns>
        Task<DbTool> AddAsync(DbTool tool);

        /// <summary>
        /// Deletes a tool from the database.
        /// </summary>
        /// <param name="tool">The tool to delete.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> DeleteAsync(DbTool tool);
    }
}
