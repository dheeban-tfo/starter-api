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
    public class UnitController : ControllerBase
    {
        private readonly IUnitRepository _unitRepository;
        private readonly ILogger<UnitController> _logger;

        public UnitController(IUnitRepository unitRepository, ILogger<UnitController> logger)
        {
            _unitRepository = unitRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<Unit>>> GetAllUnits([FromQuery] QueryParameters queryParameters)
        {
            return await _unitRepository.GetAllAsync(queryParameters);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Unit>> GetUnit(int id)
        {
            var unit = await _unitRepository.GetByIdAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            return unit;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Unit>> CreateUnit(Unit unit)
        {
            var createdUnit = await _unitRepository.CreateAsync(unit);
            return CreatedAtAction(nameof(GetUnit), new { id = createdUnit.Id }, createdUnit);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUnit(int id, Unit unit)
        {
            if (id != unit.Id)
            {
                return BadRequest();
            }

            await _unitRepository.UpdateAsync(unit);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            await _unitRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("floor/{floorId}")]
        public async Task<ActionResult<PagedResult<Unit>>> GetUnitsByFloor(int floorId, [FromQuery] QueryParameters queryParameters)
        {
            return await _unitRepository.GetByFloorAsync(floorId, queryParameters);
        }
    }
}