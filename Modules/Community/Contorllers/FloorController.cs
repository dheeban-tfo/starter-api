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
    public class FloorController : ControllerBase
    {
        private readonly IFloorRepository _floorRepository;
        private readonly ILogger<FloorController> _logger;

        public FloorController(IFloorRepository floorRepository, ILogger<FloorController> logger)
        {
            _floorRepository = floorRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<Floor>>> GetAllFloors([FromQuery] QueryParameters queryParameters)
        {
            return await _floorRepository.GetAllAsync(queryParameters);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Floor>> GetFloor(int id)
        {
            var floor = await _floorRepository.GetByIdAsync(id);
            if (floor == null)
            {
                return NotFound();
            }
            return floor;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Floor>> CreateFloor(Floor floor)
        {
            var createdFloor = await _floorRepository.CreateAsync(floor);
            return CreatedAtAction(nameof(GetFloor), new { id = createdFloor.Id }, createdFloor);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateFloor(int id, Floor floor)
        {
            if (id != floor.Id)
            {
                return BadRequest();
            }

            await _floorRepository.UpdateAsync(floor);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFloor(int id)
        {
            await _floorRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("block/{blockId}")]
        public async Task<ActionResult<PagedResult<Floor>>> GetFloorsByBlock(int blockId, [FromQuery] QueryParameters queryParameters)
        {
            return await _floorRepository.GetByBlockAsync(blockId, queryParameters);
        }
    }
}