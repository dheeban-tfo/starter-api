using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using starterapi;
using starterapi.Models;
using StarterApi.Models;
using StarterApi.Repositories;

namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Module(ModuleName.CommunityManagement)]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityRepository _communityRepository;
        private readonly ILogger<CommunityController> _logger;

        public CommunityController(
            ICommunityRepository communityRepository,
            ILogger<CommunityController> logger
        )
        {
            _communityRepository = communityRepository;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CommunityDto>> GetCommunity(int id)
        {
            var community = await _communityRepository.GetByIdAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return new CommunityDto
            {
                Id = community.Id,
                Name = community.Name,
                Address = community.Address
            };
        }

        //  [HttpGet("all")]
        // public async Task<ActionResult<IEnumerable<Community>>> GetAllCommunities()
        // {
        //     return await _communityRepository.GetAllAsync();
        // }

        [HttpGet]
        public async Task<ActionResult<PagedResult<CommunityDto>>> GetAllCommunities(
            [FromQuery] QueryParameters queryParameters
        )
        {
            var result = await _communityRepository.GetAllAsync(queryParameters);
            var dtos = new PagedResult<CommunityDto>
            {
                Items = result
                    .Items.Select(c => new CommunityDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Address = c.Address
                    })
                    .ToList(),
                TotalItems = result.TotalItems,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
            return dtos;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Community>> CreateCommunity(Community community)
        {
            community.CreatedAt = DateTime.UtcNow;
            var createdCommunity = await _communityRepository.CreateAsync(community);
            return CreatedAtAction(
                nameof(GetCommunity),
                new { id = createdCommunity.Id },
                createdCommunity
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCommunity(int id, Community community)
        {
            if (id != community.Id)
            {
                return BadRequest();
            }

            community.ModifiedAt = DateTime.UtcNow;
            await _communityRepository.UpdateAsync(community);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCommunity(int id)
        {
            await _communityRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("{id}/withBlocks")]
        public async Task<ActionResult<CommunityWithBlocksDto>> GetCommunityWithBlocks(int id)
        {
            var community = await _communityRepository.GetCommunityWithBlocksAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return community;
        }

        [HttpGet("{id}/withBlocksAndFloors")]
        public async Task<
            ActionResult<CommunityWithBlocksAndFloorsDto>
        > GetCommunityWithBlocksAndFloors(int id)
        {
            var community = await _communityRepository.GetCommunityWithBlocksAndFloorsAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return community;
        }

        [HttpGet("{id}/full")]
        public async Task<ActionResult<CommunityFullDto>> GetCommunityFull(int id)
        {
            var community = await _communityRepository.GetCommunityFullAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return community;
        }

        [HttpGet("statistics")]
        [Authorize(Policy = "PermissionPolicy")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<CommunityStatisticsDto>> GetCommunityStatistics()
        {
            try
            {
                var statistics = await _communityRepository.GetCommunityStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving community statistics");
                return StatusCode(500, "An error occurred while retrieving community statistics");
            }
        }

        [HttpGet("basic-stats")]
        public async Task<ActionResult<List<CommunityBasicStatsDto>>> GetAllCommunityBasicStats()
        {
            try
            {
                var basicStats = await _communityRepository.GetAllCommunityBasicStatsAsync();
                return Ok(basicStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving basic community statistics");
                return StatusCode(
                    500,
                    "An error occurred while retrieving basic community statistics"
                );
            }
        }
    }
}
