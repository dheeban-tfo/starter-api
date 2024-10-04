using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using starterapi;
using starterapi.Models;
using StarterApi.Models;
using StarterApi.Repositories;
using System.IO;
using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; 

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
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Create))]
        public async Task<ActionResult<CommunityDto>> CreateCommunity(CommunityDto communityDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var community = new Community
            {
                Name = communityDto.Name,
                Address = communityDto.Address,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = userId,
                IsActive = true,
                Version = DateTime.UtcNow.Ticks
            };

            var createdCommunity = await _communityRepository.CreateAsync(community);
            
            var createdCommunityDto = new CommunityDto
            {
                Id = createdCommunity.Id,
                Name = createdCommunity.Name,
                Address = createdCommunity.Address
            };

            return CreatedAtAction(
                nameof(GetCommunity),
                new { id = createdCommunityDto.Id },
                createdCommunityDto
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

        [HttpPost("{communityId}/import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportCommunityData(int communityId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty");
            }

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<CommunityImportDto>().ToList();
                    await _communityRepository.ImportCommunityDataAsync(communityId, records);
                }

                return Ok("Data imported successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing community data");
                return StatusCode(500, "An error occurred while importing community data");
            }
        }
    }
}
