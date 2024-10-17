using StarterApi.Models;

namespace StarterApi.Repositories
{
    public interface IBookingRepository
    {
        Task<IEnumerable<FacilityBookingDto>> GetAllAsync();
        Task<FacilityBookingDto> GetByIdAsync(int id);
        Task<FacilityBookingDto> CreateAsync(CreateBookingDto bookingDto);
        Task<FacilityBookingDto> UpdateAsync(int id, UpdateBookingStatusDto statusDto);
        Task DeleteAsync(int id);
    }
}