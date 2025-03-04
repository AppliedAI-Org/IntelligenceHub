using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing tool properties in the database.
    /// </summary>
    public interface IPropertyRepository
    {
        /// <summary>
        /// Retrieves all properties associated with a specific tool.
        /// </summary>
        /// <param name="toolId">The ID of the tool.</param>
        /// <returns>A list of tool properties.</returns>
        Task<IEnumerable<DbProperty>> GetToolProperties(int toolId);

        /// <summary>
        /// Adds a new property to the database.
        /// </summary>
        /// <param name="property">The property to add.</param>
        /// <returns>The new property.</returns>
        Task<DbProperty> AddAsync(DbProperty property);

        /// <summary>
        /// Deletes a property from the database.
        /// </summary>
        /// <param name="property">The property to delete.</param>
        /// <returns>A boolean indicating the success of the operation.</returns>
        Task<bool> DeleteAsync(DbProperty property);
    }
}
