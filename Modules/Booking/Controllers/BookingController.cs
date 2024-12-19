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
    [Module(ModuleName.FacilityBooking)]
    public class BookingController : ControllerBase
    {
       private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<BookingController> _logger;
        private readonly IMapper _mapper;

        public BookingController(
            IBookingRepository bookingRepository,
            ILogger<BookingController> logger,
            IMapper mapper
        )
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpPost]
        [Authorize]
        [Permission(nameof(ModuleActions.FacilityBooking.Create))]
        public async Task<ActionResult<FacilityBookingDto>> CreateBooking(CreateBookingDto bookingDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            bookingDto.UserId = Guid.Parse(userId);
            bookingDto.CreatedBy = userId;
            bookingDto.ModifiedBy = userId;

            var createdBooking = await _bookingRepository.CreateAsync(bookingDto);
            return CreatedAtAction(nameof(GetBooking), new { id = createdBooking.Id }, createdBooking);
        }

        [HttpGet("{id}")]
        [Permission(nameof(ModuleActions.FacilityBooking.Read))]
        public async Task<ActionResult<FacilityBookingDto>> GetBooking(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            return Ok(booking);
        }

        [HttpGet]
        [Permission(nameof(ModuleActions.FacilityBooking.Read))]
        public async Task<ActionResult<IEnumerable<FacilityBookingDto>>> GetAllBookings()
        {
            var bookings = await _bookingRepository.GetAllAsync();
            return Ok(bookings);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.FacilityBooking.Update))]
        public async Task<IActionResult> UpdateBookingStatus(int id, UpdateBookingStatusDto statusDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            statusDto.ModifiedBy = userId;

            var updatedBooking = await _bookingRepository.UpdateAsync(id, statusDto);
            if (updatedBooking == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        [Permission(nameof(ModuleActions.FacilityBooking.Delete))]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (booking.UserId.ToString() != userId && !User.IsInRole("Admin") && !User.IsInRole("Super Admin"))
            {
                return Forbid();
            }

            var cancelDto = new UpdateBookingStatusDto
            {
                Status = BookingStatus.Cancelled,
                ModifiedBy = userId
            };

            await _bookingRepository.UpdateAsync(id, cancelDto);
            return NoContent();
        }
    }
}