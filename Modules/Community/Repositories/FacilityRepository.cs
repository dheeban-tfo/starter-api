using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StarterApi.Models;
using starterapi;
using starterapi.Services;

namespace StarterApi.Repositories
{
    public class FacilityRepository : IFacilityRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;
        private readonly IMapper _mapper;

        public FacilityRepository(ITenantDbContextAccessor contextAccessor, IMapper mapper)
        {
            _contextAccessor = contextAccessor;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FacilityDto>> GetAllAsync()
        {
            var facilities = await _contextAccessor.TenantDbContext.Facilities.ToListAsync();
            return _mapper.Map<IEnumerable<FacilityDto>>(facilities);
        }

        public async Task<FacilityDto> GetByIdAsync(int id)
        {
            var facility = await _contextAccessor.TenantDbContext.Facilities.FindAsync(id);
            return _mapper.Map<FacilityDto>(facility);
        }

        public async Task<FacilityDto> CreateAsync(CreateFacilityDto facilityDto)
        {
            var facility = _mapper.Map<Facility>(facilityDto);
            _contextAccessor.TenantDbContext.Facilities.Add(facility);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return _mapper.Map<FacilityDto>(facility);
        }

        public async Task<FacilityDto> UpdateAsync(int id, UpdateFacilityDto facilityDto)
        {
            var facility = await _contextAccessor.TenantDbContext.Facilities.FindAsync(id);
            if (facility == null)
                return null;

            _mapper.Map(facilityDto, facility);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return _mapper.Map<FacilityDto>(facility);
        }

        public async Task DeleteAsync(int id)
        {
            var facility = await _contextAccessor.TenantDbContext.Facilities.FindAsync(id);
            if (facility != null)
            {
                _contextAccessor.TenantDbContext.Facilities.Remove(facility);
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
            }
        }
    }
}
