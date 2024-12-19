using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi.Modules;
using starterapi.Modules.Extensions;
using starterapi.Services;

namespace starterapi;

[ApiVersion("1.0")]
[Module(ModuleName.UserManagement)]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Super Admin")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly ILogger<UserController> _logger;
    private readonly IEmailVerificationService _emailVerificationService;

    public UserController(
        IUserRepository userRepository,
        ITenantDbContextAccessor contextAccessor,
        ILogger<UserController> logger,
        IEmailVerificationService emailVerificationService
    )
    {
        _userRepository = userRepository;
        _contextAccessor = contextAccessor;
        _logger = logger;
        _emailVerificationService = emailVerificationService;
    }

    [Permission("View")]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(Guid id)
    {
        _logger.LogInformation("GetUserById");
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    
    [HttpGet]
    [Authorize(Policy = "PermissionPolicy")]
    [Permission(nameof(ModuleActions.UserManagement.Read))]
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

        var context = _contextAccessor.TenantDbContext;
        var basicRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Basic");
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
    public async Task<IActionResult> UpdateUser(Guid id, User user)
    {
        if (id != user.Id)
            return BadRequest();

        await _userRepository.UpdateUserAsync(user);
        return NoContent();
    }

    [Permission("Delete")]
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        await _userRepository.DeactivateUserAsync(id);
        return NoContent();
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
        return Ok("Test successful");
    }
}
