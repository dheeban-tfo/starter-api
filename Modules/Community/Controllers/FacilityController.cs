using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using starterapi;
using starterapi.Modules;
using starterapi.Modules.Extensions;
using StarterApi.Models;
using StarterApi.Repositories;
using System.Security.Claims;

namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Module(ModuleName.FacilityManagement)]
    public class FacilityController : ControllerBase
    {
        private readonly IFacilityRepository _facilityRepository;
        private readonly ILogger<FacilityController> _logger;
        private readonly IMapper _mapper;

        public FacilityController(
            IFacilityRepository facilityRepository,
            ILogger<FacilityController> logger,
            IMapper mapper
        )
        {
            _facilityRepository = facilityRepository;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [Permission(nameof(ModuleActions.FacilityManagement.Read))]
        public async Task<ActionResult<IEnumerable<FacilityDto>>> GetAllFacilities()
        {
            var facilities = await _facilityRepository.GetAllAsync();
            return Ok(facilities);
        }

        [HttpGet("{id}")]
        [Permission(nameof(ModuleActions.FacilityManagement.Read))]
        public async Task<ActionResult<FacilityDto>> GetFacility(int id)
        {
            var facility = await _facilityRepository.GetByIdAsync(id);
            if (facility == null)
            {
                return NotFound();
            }
            return Ok(facility);
        }


        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.FacilityManagement.Create))]
        public async Task<ActionResult<FacilityDto>> CreateFacility(CreateFacilityDto facilityDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

         

            var createdFacility = await _facilityRepository.CreateAsync(facilityDto);
            return CreatedAtAction(nameof(GetFacility), new { id = createdFacility.Id }, createdFacility);
        }

         [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.FacilityManagement.Update))]
        public async Task<IActionResult> UpdateFacility(int id, UpdateFacilityDto facilityDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            //facilityDto.ModifiedBy = userId;

            var updatedFacility = await _facilityRepository.UpdateAsync(id, facilityDto);
            if (updatedFacility == null)
            {
                return NotFound();
            }

            return NoContent();
        }
        

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.FacilityManagement.Delete))]
        public async Task<IActionResult> DeleteFacility(int id)
        {
            var facility = await _facilityRepository.GetByIdAsync(id);
            if (facility == null)
            {
                return NotFound();
            }

            await _facilityRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}