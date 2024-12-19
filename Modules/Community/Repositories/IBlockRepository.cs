using StarterApi.Models;
using starterapi.Models;
using StarterApi.Models.Communities;

namespace StarterApi.Repositories
{
    public interface IBlockRepository
    {
        Task<Block> GetByIdAsync(int id);
        Task<PagedResult<Block>> GetAllAsync(QueryParameters queryParameters);
        Task<Block> CreateAsync(Block block);
        Task<Block> UpdateAsync(Block block);
        Task DeleteAsync(int id);
        Task<PagedResult<Block>> GetByCommunityAsync(int communityId, QueryParameters queryParameters);
    }
}