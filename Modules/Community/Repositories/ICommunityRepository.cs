using starterapi;
using starterapi.Models;
using StarterApi.Models;

namespace StarterApi.Repositories
{
    public interface ICommunityRepository
    {
        Task<CommunityDto> GetByIdAsync(int id);
       // Task<List<CommunityDto>> GetAllAsync();
        Task<PagedResult<CommunityDto>> GetAllAsync(QueryParameters queryParameters);
        Task<Community> CreateAsync(Community community);
        Task<Community> UpdateAsync(Community community);
        Task DeleteAsync(int id);
        Task<CommunityWithBlocksDto> GetCommunityWithBlocksAsync(int id);
        Task<CommunityWithBlocksAndFloorsDto> GetCommunityWithBlocksAndFloorsAsync(int id);
        Task<CommunityFullDto> GetCommunityFullAsync(int id);
        Task<CommunityStatisticsDto> GetCommunityStatisticsAsync();
        Task<List<CommunityBasicStatsDto>> GetAllCommunityBasicStatsAsync();
    }
}
