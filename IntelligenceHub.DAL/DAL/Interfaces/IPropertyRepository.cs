using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<DbProperty>> GetToolProperties(int toolId);
        Task AddAsync(DbProperty property);
        Task DeleteAsync(DbProperty property);
    }
}
