using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StarterApi.Models;
using starterapi;
using starterapi.Services;

namespace StarterApi.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;
        private readonly IMapper _mapper;

        public BookingRepository(ITenantDbContextAccessor contextAccessor, IMapper mapper)
        {
            _contextAccessor = contextAccessor;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FacilityBookingDto>> GetAllAsync()
        {
            var bookings = await _contextAccessor.TenantDbContext.FacilityBookings.ToListAsync();
            return _mapper.Map<IEnumerable<FacilityBookingDto>>(bookings);
        }

        public async Task<FacilityBookingDto> GetByIdAsync(int id)
        {
            var booking = await _contextAccessor.TenantDbContext.FacilityBookings.FindAsync(id);
            return _mapper.Map<FacilityBookingDto>(booking);
        }

        public async Task<FacilityBookingDto> CreateAsync(CreateBookingDto bookingDto)
        {
            var booking = _mapper.Map<FacilityBooking>(bookingDto);
            _contextAccessor.TenantDbContext.FacilityBookings.Add(booking);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return _mapper.Map<FacilityBookingDto>(booking);
        }

        public async Task<FacilityBookingDto> UpdateAsync(int id, UpdateBookingStatusDto statusDto)
        {
            var booking = await _contextAccessor.TenantDbContext.FacilityBookings.FindAsync(id);
            if (booking == null)
                return null;

            _mapper.Map(statusDto, booking);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return _mapper.Map<FacilityBookingDto>(booking);
        }

        public async Task DeleteAsync(int id)
        {
            var booking = await _contextAccessor.TenantDbContext.FacilityBookings.FindAsync(id);
            if (booking != null)
            {
                _contextAccessor.TenantDbContext.FacilityBookings.Remove(booking);
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
            }
        }
    }
}
