using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StarterApi.Models;
using starterapi;

namespace StarterApi.Repositories
{
    public class FacilityRepository : IFacilityRepository
    {
        private readonly TenantDbContext _context;
        private readonly IMapper _mapper;

        public FacilityRepository(TenantDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FacilityDto>> GetAllAsync()
        {
            var facilities = await _context.Facilities.ToListAsync();
            return _mapper.Map<IEnumerable<FacilityDto>>(facilities);
        }

        public async Task<FacilityDto> GetByIdAsync(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            return _mapper.Map<FacilityDto>(facility);
        }

        public async Task<FacilityDto> CreateAsync(CreateFacilityDto facilityDto)
        {
            var facility = _mapper.Map<Facility>(facilityDto);
            _context.Facilities.Add(facility);
            await _context.SaveChangesAsync();
            return _mapper.Map<FacilityDto>(facility);
        }

        public async Task<FacilityDto> UpdateAsync(int id, UpdateFacilityDto facilityDto)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null)
                return null;

            _mapper.Map(facilityDto, facility);
            await _context.SaveChangesAsync();
            return _mapper.Map<FacilityDto>(facility);
        }

        public async Task DeleteAsync(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility != null)
            {
                _context.Facilities.Remove(facility);
                await _context.SaveChangesAsync();
            }
        }
    }
}