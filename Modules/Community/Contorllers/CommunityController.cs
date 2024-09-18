using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterApi.Models;
using StarterApi.Repositories;

namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityRepository _communityRepository;
        private readonly ILogger<CommunityController> _logger;

        public CommunityController(ICommunityRepository communityRepository, ILogger<CommunityController> logger)
        {
            _communityRepository = communityRepository;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Community>> GetCommunity(int id)
        {
            var community = await _communityRepository.GetByIdAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return community;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Community>>> GetAllCommunities()
        {
            return await _communityRepository.GetAllAsync();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Community>> CreateCommunity(Community community)
        {
            community.CreatedAt = DateTime.UtcNow;
            var createdCommunity = await _communityRepository.CreateAsync(community);
            return CreatedAtAction(nameof(GetCommunity), new { id = createdCommunity.Id }, createdCommunity);
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
    }
}