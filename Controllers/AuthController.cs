using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using starterapi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace starterapi;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailVerificationService _emailVerificationService;

    public AuthController(
        IUserRepository userRepository, 
        IJwtService jwtService, 
        ILogger<AuthController> logger,
        IEmailVerificationService emailVerificationService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
        _emailVerificationService = emailVerificationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var user = await _userRepository.GetUserByEmailAsync(loginRequest.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.EmailVerified)
            {
                // Generate a new verification token and send a new verification email
                await _emailVerificationService.GenerateVerificationTokenAsync(user);
                return Unauthorized(new { message = "Email not verified. A new verification email has been sent." });
            }

            var token = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await _userRepository.UpdateUserAsync(user);

            return Ok(new { Token = token, RefreshToken = refreshToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login");
            return StatusCode(500, new { message = "An error occurred during login. Please try again later." });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        try
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);

            if (user == null)
            {
                // To prevent email enumeration, we'll return a success message even if the email doesn't exist
                return Ok(new { message = "If the email exists in our system, a new verification email has been sent." });
            }

            if (user.EmailVerified)
            {
                return BadRequest(new { message = "Email is already verified." });
            }

            await _emailVerificationService.GenerateVerificationTokenAsync(user);
            return Ok(new { message = "A new verification email has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resending verification email");
            return StatusCode(500, new { message = "An error occurred. Please try again later." });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshRequest)
    {
        if (refreshRequest is null)
        {
            return BadRequest("Invalid client request");
        }

        string accessToken = refreshRequest.AccessToken;
        string refreshToken = refreshRequest.RefreshToken;

        var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
        {
            return BadRequest("Invalid access token or refresh token");
        }

        string userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _userRepository.GetUserByIdAsync(int.Parse(userId));

        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
        {
            return BadRequest("Invalid access token or refresh token");
        }

        var newAccessToken = _jwtService.GenerateToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        await _userRepository.UpdateUserAsync(user);

        return new ObjectResult(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }

    [HttpPost("revoke/{userId}")]
    [Authorize]
    public async Task<IActionResult> Revoke(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) return BadRequest("Invalid user id");

        user.RefreshToken = null;
        await _userRepository.UpdateUserAsync(user);

        return NoContent();
    }
}

public class ResendVerificationRequest
{
    public string Email { get; set; }
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}