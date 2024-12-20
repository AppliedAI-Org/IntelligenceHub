namespace IntelligenceHub.DAL.Interfaces
{
    public interface IGenericRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync(int? count = null, int? page = null);
        Task<T> AddAsync(T entity);
        Task<int> UpdateAsync(T existingEntity, T entity);
        Task<int> DeleteAsync(T entity);
    }
}