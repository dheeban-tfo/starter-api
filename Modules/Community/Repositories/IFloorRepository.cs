using StarterApi.Models;
using starterapi.Models;
using StarterApi.Models.Communities;

namespace StarterApi.Repositories
{
    public interface IFloorRepository
    {
        Task<Floor> GetByIdAsync(int id);
        Task<PagedResult<Floor>> GetAllAsync(QueryParameters queryParameters);
        Task<Floor> CreateAsync(Floor floor);
        Task<Floor> UpdateAsync(Floor floor);
        Task DeleteAsync(int id);
        Task<PagedResult<Floor>> GetByBlockAsync(int blockId, QueryParameters queryParameters);
    }
}