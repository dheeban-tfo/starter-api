using AutoMapper;
using starterapi.Models;
using StarterApi.Models.Communities;

public class UnitMappingProfile : Profile
{
    public UnitMappingProfile()
    {
        CreateMap<Unit, UnitDto>()
            .ForMember(dest => dest.FloorNumber, 
                opt => opt.MapFrom(src => src.Floor.FloorNumber))
            .ForMember(dest => dest.BlockName, 
                opt => opt.MapFrom(src => src.Floor.Block.Name));

        CreateMap<CreateUnitDto, Unit>();
        CreateMap<UpdateUnitDto, Unit>();
        CreateMap<UnitOwnership, UnitOwnershipDto>();
        CreateMap<UnitResident, UnitResidentDto>();

        CreateMap<PagedResult<Unit>, PagedResult<UnitDto>>();
    }
}