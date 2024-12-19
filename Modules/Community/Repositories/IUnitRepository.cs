using StarterApi.Models;
using starterapi.Models;
using StarterApi.Models.Communities;

namespace StarterApi.Repositories
{
    public interface IUnitRepository
    {
        Task<Unit> GetByIdAsync(int id);
        Task<Unit> GetByIdWithDetailsAsync(int id);
        Task<PagedResult<Unit>> GetAllAsync(QueryParameters queryParameters);
        Task<PagedResult<Unit>> GetAllWithDetailsAsync(QueryParameters queryParameters);
        Task<Unit> CreateAsync(Unit unit);
        Task<Unit> UpdateAsync(Unit unit);
        Task DeleteAsync(int id);
        Task<PagedResult<Unit>> GetByFloorAsync(int floorId, QueryParameters queryParameters);
        Task<bool> ExistsAsync(int id);
        Task<bool> IsUnitNumberUniqueAsync(string unitNumber, int floorId, int? excludeUnitId = null);
    }
}