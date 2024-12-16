using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using starterapi.Models;
using starterapi.Services;
using System.Security.Claims;

namespace starterapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileService _profileService;
    private readonly ITenantDbContextAccessor _dbContextAccessor;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IUserRepository userRepository,
        IProfileService profileService,
        ITenantDbContextAccessor dbContextAccessor,
        ILogger<ProfileController> logger)
    {
        _userRepository = userRepository;
        _profileService = profileService;
        _dbContextAccessor = dbContextAccessor;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> GetUserProfile()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var tenantId = User.FindFirst("TenantId")?.Value;

            _logger.LogInformation("Retrieving profile for UserId: {UserId}, TenantId: {TenantId}", userId, tenantId);

            var tenantDb = _dbContextAccessor.TenantDbContext;
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User not found. UserId: {UserId}, TenantId: {TenantId}", userId, tenantId);
                return NotFound("User not found.");
            }

            var userProfile = await _profileService.GetUserProfileAsync(user);
            return Ok(userProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, "An error occurred while retrieving the user profile.");
        }
    }
}