using StarterApi.Models;
using starterapi.Models;

namespace StarterApi.Repositories
{
    public interface IUnitRepository
    {
        Task<Unit> GetByIdAsync(int id);
        Task<PagedResult<Unit>> GetAllAsync(QueryParameters queryParameters);
        Task<Unit> CreateAsync(Unit unit);
        Task<Unit> UpdateAsync(Unit unit);
        Task DeleteAsync(int id);
        Task<PagedResult<Unit>> GetByFloorAsync(int floorId, QueryParameters queryParameters);
    }
}