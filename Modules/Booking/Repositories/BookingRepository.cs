using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StarterApi.Models;
using starterapi;

namespace StarterApi.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly TenantDbContext _context;
        private readonly IMapper _mapper;

        public BookingRepository(TenantDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FacilityBookingDto>> GetAllAsync()
        {
            var bookings = await _context.FacilityBookings.ToListAsync();
            return _mapper.Map<IEnumerable<FacilityBookingDto>>(bookings);
        }

        public async Task<FacilityBookingDto> GetByIdAsync(int id)
        {
            var booking = await _context.FacilityBookings.FindAsync(id);
            return _mapper.Map<FacilityBookingDto>(booking);
        }

        public async Task<FacilityBookingDto> CreateAsync(CreateBookingDto bookingDto)
        {
            var booking = _mapper.Map<FacilityBooking>(bookingDto);
            _context.FacilityBookings.Add(booking);
            await _context.SaveChangesAsync();
            return _mapper.Map<FacilityBookingDto>(booking);
        }

        public async Task<FacilityBookingDto> UpdateAsync(int id, UpdateBookingStatusDto statusDto)
        {
            var booking = await _context.FacilityBookings.FindAsync(id);
            if (booking == null)
                return null;

            _mapper.Map(statusDto, booking);
            await _context.SaveChangesAsync();
            return _mapper.Map<FacilityBookingDto>(booking);
        }

        public async Task DeleteAsync(int id)
        {
            var booking = await _context.FacilityBookings.FindAsync(id);
            if (booking != null)
            {
                _context.FacilityBookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }
    }
}