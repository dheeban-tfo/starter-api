using StarterApi.Models;

namespace StarterApi.Repositories
{
    public interface IFacilityRepository
    {
        Task<IEnumerable<FacilityDto>> GetAllAsync();
        Task<FacilityDto> GetByIdAsync(int id);
        Task<FacilityDto> CreateAsync(CreateFacilityDto facilityDto);
        Task<FacilityDto> UpdateAsync(int id, UpdateFacilityDto facilityDto);
        Task DeleteAsync(int id);
    }
}