using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace starterapi.Controllers.V2;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserV2>>> GetUsers()
    {
        var users = await _userRepository.GetUsersAsync();
        var usersV2 = users.Select(u => new UserV2 
        { 
            Id = u.Id, 
            FullName = $"{u.FirstName} {u.LastName}", 
            Email = u.Email 
        });
        return Ok(usersV2);
    }

    // ... other methods ...
}

public class UserV2
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}