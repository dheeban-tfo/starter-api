using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarterApi.Models;
using StarterApi.Repositories;
using starterapi.Models;
using StarterApi.Models.Communities;

using StarterApi.DTOs;
using starterapi.Modules.Extensions;
using starterapi.Modules;

namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Module(ModuleName.FacilityManagement)]
    public class UnitController : ControllerBase
    {
        private readonly IUnitService _unitService;
        private readonly ILogger<UnitController> _logger;

        public UnitController(IUnitService unitService, ILogger<UnitController> logger)
        {
            _unitService = unitService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<PagedResult<UnitDto>>> GetAllUnits([FromQuery] QueryParameters queryParameters)
        {
            var units = await _unitService.GetAllAsync(queryParameters);
            return Ok(units);
        }

        [HttpGet("{id}")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<UnitDto>> GetUnit(int id)
        {
            var unit = await _unitService.GetByIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            return Ok(unit);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Create))]
        public async Task<ActionResult<UnitDto>> CreateUnit(CreateUnitDto createUnitDto)
        {
            var unit = await _unitService.CreateAsync(createUnitDto);
            return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, unit);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Update))]
        public async Task<IActionResult> UpdateUnit(int id, UpdateUnitDto updateUnitDto)
        {
            await _unitService.UpdateAsync(id, updateUnitDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Delete))]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            await _unitService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("floor/{floorId}")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<PagedResult<UnitDto>>> GetUnitsByFloor(
            int floorId, 
            [FromQuery] QueryParameters queryParameters)
        {
            var units = await _unitService.GetByFloorAsync(floorId, queryParameters);
            return Ok(units);
        }
    }
}