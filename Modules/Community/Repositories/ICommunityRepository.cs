using StarterApi.Models;

namespace StarterApi.Repositories
{
    public interface ICommunityRepository
    {
        Task<Community> GetByIdAsync(int id);
        Task<List<Community>> GetAllAsync();
        Task<Community> CreateAsync(Community community);
        Task<Community> UpdateAsync(Community community);
        Task DeleteAsync(int id);
    }
}