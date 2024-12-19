using AutoMapper;
using Modules.Community.Services;
using starterapi.Models;
using StarterApi.Models.Communities;
using System.Collections.Generic;

public class FloorMappingProfile : Profile
{
    public FloorMappingProfile()
    {
        CreateMap<Floor, FloorDto>()
            .ForMember(dest => dest.BlockName, 
                opt => opt.MapFrom(src => src.Block.Name))
            .ForMember(dest => dest.NumberOfUnits, 
                opt => opt.MapFrom(src => src.Units.Count));

        CreateMap<CreateFloorDto, Floor>();
        CreateMap<UpdateFloorDto, Floor>();

        CreateMap<PagedResult<Floor>, PagedResult<FloorDto>>();
    }
} 