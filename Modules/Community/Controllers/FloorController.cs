using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterApi.Models;
using StarterApi.Repositories;
using starterapi.Models;
using StarterApi.Models.Communities;
using starterapi.Modules.Extensions;
using starterapi.Modules;


namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Module(ModuleName.FacilityManagement)]
    public class FloorController : ControllerBase
    {
        private readonly IFloorService _floorService;
        private readonly ILogger<FloorController> _logger;

        public FloorController(IFloorService floorService, ILogger<FloorController> logger)
        {
            _floorService = floorService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<PagedResult<FloorDto>>> GetAllFloors([FromQuery] QueryParameters queryParameters)
        {
            var floors = await _floorService.GetAllAsync(queryParameters);
            return Ok(floors);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<FloorDto>> GetFloor(int id)
        {
            var floor = await _floorService.GetByIdAsync(id);
            if (floor == null)
            {
                return NotFound();
            }
            return Ok(floor);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Create))]
        public async Task<ActionResult<FloorDto>> CreateFloor(CreateFloorDto createFloorDto)
        {
            var floor = await _floorService.CreateAsync(createFloorDto);
            return CreatedAtAction(nameof(GetFloor), new { id = floor.Id }, floor);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Update))]
        public async Task<IActionResult> UpdateFloor(int id, UpdateFloorDto updateFloorDto)
        {
            await _floorService.UpdateAsync(id, updateFloorDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Delete))]
        public async Task<IActionResult> DeleteFloor(int id)
        {
            await _floorService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("block/{blockId}")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<PagedResult<FloorDto>>> GetFloorsByBlock(
            int blockId, 
            [FromQuery] QueryParameters queryParameters)
        {
            var floors = await _floorService.GetByBlockAsync(blockId, queryParameters);
            return Ok(floors);
        }
    }
}