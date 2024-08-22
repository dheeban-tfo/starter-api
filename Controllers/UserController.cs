using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi.Services;

namespace starterapi;

[ApiVersion("1.0")]
[Module("User Management")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserController> _logger;
    private readonly IEmailVerificationService _emailVerificationService;

    public UserController(
        IUserRepository userRepository,
        ApplicationDbContext context,
        ILogger<UserController> logger,
        IEmailVerificationService emailVerificationService
    )
    {
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
        _emailVerificationService = emailVerificationService;
    }

    [Permission("View")]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(int id)
    {
        _logger.LogInformation("GetUserById");
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    [Permission("View")]
    [Authorize(Policy = "PermissionPolicy")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return Ok(await _userRepository.GetUsersAsync());
    }

    [Permission("Create")]
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult> CreateUser(UserCreateRequest request)
    {
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) // Hash the plain text password
        };

        // Retrieve the Basic role from the database
        var basicRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Basic");
        if (basicRole != null)
        {
            user.UserRoles = new List<UserRole>
            {
                new UserRole { RoleId = basicRole.Id, User = user }
            };
        }

        await _userRepository.AddUserAsync(user);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [Permission("Edit")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.Id)
            return BadRequest();

        await _userRepository.UpdateUserAsync(user);
        return NoContent();
    }

    [Permission("Delete")]
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        await _userRepository.DeactivateUserAsync(id);
        return NoContent();
    }
}
