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

    public ProfileController(IUserRepository userRepository, IProfileService profileService)
    {
        _userRepository = userRepository;
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> GetUserProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userRepository.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        var userProfile = await _profileService.GetUserProfileAsync(user);
        return Ok(userProfile);
    }
}