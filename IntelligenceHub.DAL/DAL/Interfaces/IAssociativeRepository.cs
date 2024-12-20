namespace IntelligenceHub.DAL.Interfaces
{
    public interface IAssociativeRepository<T>
    {
        Task<bool> AddAssociationsByProfileIdAsync(int id, List<int> toolIDs);
        Task<int> DeleteAllProfileAssociationsAsync(int id);
    }
}