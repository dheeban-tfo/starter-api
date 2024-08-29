using Microsoft.AspNetCore.Mvc;
using starterapi.Services;
using System.Threading.Tasks;

namespace starterapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;

    public PasswordResetController(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDto request)
    {
        await _passwordResetService.RequestPasswordResetAsync(request.Email);
        return Ok(new { message = "If the email exists, a password reset link has been sent." });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto reset)
    {
        var result = await _passwordResetService.ResetPasswordAsync(reset.Email, reset.Token, reset.NewPassword);
        if (result)
        {
            return Ok(new { message = "Password has been reset successfully." });
        }
        return BadRequest(new { message = "Invalid or expired password reset token." });
    }
}

public class PasswordResetRequestDto
{
    public string Email { get; set; }
}

public class PasswordResetDto
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}