using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterApi.DTOs;
using StarterApi.Repositories;
using AutoMapper;
using starterapi.Models;
using starterapi.Modules;
using starterapi.Modules.Extensions;
using StarterApi.Models;
using StarterApi.Models.Communities;



namespace StarterApi.Controllers
{
     [ApiController]
    [Route("api/[controller]")]
    [Module(ModuleName.FacilityManagement)]
    public class BlockController : ControllerBase
    {
        private readonly IBlockRepository _blockRepository;
        private readonly ILogger<BlockController> _logger;
        private readonly IMapper _mapper;

        public BlockController(
            IBlockRepository blockRepository, 
            ILogger<BlockController> logger,
            IMapper mapper)
        {
            _blockRepository = blockRepository;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<ActionResult<PagedResult<BlockDto>>> GetAllBlocks([FromQuery] QueryParameters queryParameters)
        {
            var blocks = await _blockRepository.GetAllAsync(queryParameters);
            var blockDtos = _mapper.Map<PagedResult<BlockDto>>(blocks);
            return Ok(blockDtos);
        }

        [HttpGet("{id}")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<BlockDto>> GetBlock(int id)
        {
            var block = await _blockRepository.GetByIdAsync(id);
            if (block == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<BlockDto>(block));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Create))]
        public async Task<ActionResult<BlockDto>> CreateBlock(CreateBlockDto createBlockDto)
        {
            var block = _mapper.Map<Block>(createBlockDto);
            var createdBlock = await _blockRepository.CreateAsync(block);
            var blockDto = _mapper.Map<BlockDto>(createdBlock);
            return CreatedAtAction(nameof(GetBlock), new { id = blockDto.Id }, blockDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Update))]
        public async Task<IActionResult> UpdateBlock(int id, UpdateBlockDto updateBlockDto)
        {
            var existingBlock = await _blockRepository.GetByIdAsync(id);
            if (existingBlock == null)
            {
                return NotFound();
            }

            _mapper.Map(updateBlockDto, existingBlock);
            await _blockRepository.UpdateAsync(existingBlock);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Delete))]
        public async Task<IActionResult> DeleteBlock(int id)
        {
            var block = await _blockRepository.GetByIdAsync(id);
            if (block == null)
            {
                return NotFound();
            }

            await _blockRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("community/{communityId}")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<PagedResult<BlockDto>>> GetBlocksByCommunity(
            int communityId, 
            [FromQuery] QueryParameters queryParameters)
        {
            var blocks = await _blockRepository.GetByCommunityAsync(communityId, queryParameters);
            var blockDtos = _mapper.Map<PagedResult<BlockDto>>(blocks);
            return Ok(blockDtos);
        }
    }
}