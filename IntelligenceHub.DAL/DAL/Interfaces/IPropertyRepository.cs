using IntelligenceHub.DAL.Models;

namespace IntelligenceHub.DAL.Interfaces
{
    public interface IPropertyRepository
    {
        Task<IEnumerable<DbProperty>> GetToolProperties(int toolId);
        Task<DbProperty> AddAsync(DbProperty property);
        Task<int> DeleteAsync(DbProperty property);
    }
}
