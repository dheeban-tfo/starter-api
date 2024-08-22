using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace starterapi;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository userRepository, IJwtService jwtService, ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
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

            var token = _jwtService.GenerateToken(user);
            return Ok(new { Token = token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login");
            return StatusCode(500, new { message = "An error occurred during login. Please try again later." });
        }
    }
}