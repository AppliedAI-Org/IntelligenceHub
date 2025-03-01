using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Repository for managing index metadata in the database.
    /// </summary>
    public interface IIndexMetaRepository
    {
        /// <summary>
        /// Retrieves an index metadata entity by its name.
        /// </summary>
        /// <param name="name">The name of the index.</param>
        /// <returns>The index's metadata.</returns>
        Task<DbIndexMetadata?> GetByNameAsync(string name);

        /// <summary>
        /// Adds a new index metadata entity to the database.
        /// </summary>
        /// <param name="entity">The new index metadata entity.</param>
        /// <returns>The newly created entity.</returns>
        Task<DbIndexMetadata> AddAsync(DbIndexMetadata entity);

        /// <summary>
        /// Updates an existing index metadata entity in the database.
        /// </summary>
        /// <param name="entity">The new definition of the entity.</param>
        /// <returns>The new entity.</returns>
        Task<int> UpdateAsync(DbIndexMetadata entity);

        /// <summary>
        /// Retrieves all index metadata entities from the database.
        /// </summary>
        /// <param name="count">The number of entities to retrieve.</param>
        /// <param name="page">The page number to offset the results by.</param>
        /// <returns>A list of all index metadata entities.</returns>
        Task<IEnumerable<DbIndexMetadata>> GetAllAsync(int? count = null, int? page = null);

        /// <summary>
        /// Deletes an index metadata entity from the database.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> DeleteAsync(DbIndexMetadata entity);
    }
}
