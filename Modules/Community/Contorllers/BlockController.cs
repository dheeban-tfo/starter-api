using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterApi.Models;
using StarterApi.Repositories;
using starterapi.Models;

namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Root,Admin")]
    public class BlockController : ControllerBase
    {
        private readonly IBlockRepository _blockRepository;
        private readonly ILogger<BlockController> _logger;

        public BlockController(IBlockRepository blockRepository, ILogger<BlockController> logger)
        {
            _blockRepository = blockRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<Block>>> GetAllBlocks([FromQuery] QueryParameters queryParameters)
        {
            return await _blockRepository.GetAllAsync(queryParameters);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Block>> GetBlock(int id)
        {
            var block = await _blockRepository.GetByIdAsync(id);
            if (block == null)
            {
                return NotFound();
            }
            return block;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Block>> CreateBlock(Block block)
        {
            var createdBlock = await _blockRepository.CreateAsync(block);
            return CreatedAtAction(nameof(GetBlock), new { id = createdBlock.Id }, createdBlock);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBlock(int id, Block block)
        {
            if (id != block.Id)
            {
                return BadRequest();
            }

            await _blockRepository.UpdateAsync(block);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBlock(int id)
        {
            await _blockRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("community/{communityId}")]
        public async Task<ActionResult<PagedResult<Block>>> GetBlocksByCommunity(int communityId, [FromQuery] QueryParameters queryParameters)
        {
            return await _blockRepository.GetByCommunityAsync(communityId, queryParameters);
        }
    }
}