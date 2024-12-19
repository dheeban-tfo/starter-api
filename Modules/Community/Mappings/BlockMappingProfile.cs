using AutoMapper;
using starterapi.Models;
using StarterApi.DTOs;
using StarterApi.Models;
using StarterApi.Models.Communities;

namespace StarterApi.Mappings
{
    public class BlockMappingProfile : Profile
    {
        public BlockMappingProfile()
        {
            CreateMap<Block, BlockDto>()
                .ForMember(dest => dest.CommunityName, 
                    opt => opt.MapFrom(src => src.Community.Name))
                .ForMember(dest => dest.NumberOfFloors, 
                    opt => opt.MapFrom(src => src.Floors.Count));
                    

            CreateMap<CreateBlockDto, Block>();
            CreateMap<UpdateBlockDto, Block>();

            CreateMap<PagedResult<Block>, PagedResult<BlockDto>>();
        }
    }
} 