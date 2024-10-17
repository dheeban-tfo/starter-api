using AutoMapper;
using StarterApi.Models;

namespace StarterApi.Mappings
{
    public class FacilityMappingProfile : Profile
    {
        public FacilityMappingProfile()
        {
            CreateMap<Facility, FacilityDto>();
            CreateMap<CreateFacilityDto, Facility>();
            CreateMap<UpdateFacilityDto, Facility>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}