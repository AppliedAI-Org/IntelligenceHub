namespace IntelligenceHub.DAL.Interfaces
{
    /// <summary>
    /// Generic repository for CRUD operations on entities of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGenericRepository<T>
    {
        /// <summary>
        /// Retrieves all entities of type T from the database.
        /// </summary>
        /// <param name="count">The number of entities to retrieve.</param>
        /// <param name="page">The page used to offset the collection.</param>
        /// <returns>A collection of entities of type T.</returns>
        Task<IEnumerable<T>> GetAllAsync(int? count = null, int? page = null);

        /// <summary>
        /// Adds a new entity of type T to the database.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The successfully added entity.</returns>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Updates an existing entity of type T in the database.
        /// </summary>
        /// <param name="existingEntity">A definition of the existing entity.</param>
        /// <param name="entity">The updated entity.</param>
        /// <returns>An int representing the number of rows affected.</returns>
        Task<int> UpdateAsync(T existingEntity, T entity);

        /// <summary>
        /// Deletes an entity of type T from the database.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <returns>An int representing the number of rows affected.</returns>
        Task<int> DeleteAsync(T entity);
    }
}