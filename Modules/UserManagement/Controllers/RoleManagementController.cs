using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi.DTOs;
using starterapi.Services;

namespace starterapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleManagementController : ControllerBase
    {
        private readonly ITenantDbContextAccessor _contextAccessor;
        private readonly ILogger<RoleManagementController> _logger;

        public RoleManagementController(
            ITenantDbContextAccessor contextAccessor,
            ILogger<RoleManagementController> logger
        )
        {
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        // Create a new role
        [HttpPost("CreateRole")]
        public async Task<ActionResult<RoleDTO>> CreateRole([FromBody] CreateRoleRequest request)
        {
            var context = _contextAccessor.TenantDbContext;
            if (await context.Roles.AnyAsync(r => r.Name == request.Name))
            {
                return BadRequest("Role already exists.");
            }

            var role = new Role { Name = request.Name };

            context.Roles.Add(role);
            await context.SaveChangesAsync();

            return Ok(new RoleDTO { Id = role.Id, Name = role.Name });
        }

        [HttpGet("modules")]
        public async Task<IActionResult> GetModulesAndActions()
        {
            var context = _contextAccessor.TenantDbContext;
            var modules = await context.Modules
                .Include(m => m.Actions)
                .Select(m => new
                {
                    Id = m.Id,
                    Name = m.Name,
                    Actions = m.Actions.Select(a => new
                    {
                        Id = a.Id,
                        Name = a.Name
                    })
                })
                .ToListAsync();

            return Ok(modules);
        }

        [HttpPost("AssignPermissionToRole")]
        public async Task<IActionResult> AssignPermissionToRole([FromBody] RoleModulePermissionDTO request)
        {
            var context = _contextAccessor.TenantDbContext;
            var role = await context.Roles
                .Include(r => r.AllowedActions)
                .FirstOrDefaultAsync(r => r.Id == request.RoleId);

            if (role == null)
                return NotFound("Role not found");

            var moduleAction = await context.ModuleActions
                .FirstOrDefaultAsync(ma => ma.Id == request.ActionId
                                          && ma.ModuleId == request.ModuleId);

            if (moduleAction == null)
                return NotFound("Module action not found");

            if (role.AllowedActions == null)
                role.AllowedActions = new List<ModuleAction>();

            if (role.AllowedActions.Any(ma => ma.Id == moduleAction.Id))
                return BadRequest("Permission already assigned to this role.");

            role.AllowedActions.Add(moduleAction);
            await context.SaveChangesAsync();

            return Ok(
                new RoleModulePermissionDTO
                {
                    RoleId = role.Id,
                    ModuleId = moduleAction.ModuleId,
                    ActionId = moduleAction.Id,
                   // ModuleName = moduleAction.Module.Name
                }
            );
        }

        [HttpDelete("RemovePermissionFromRole")]
        public async Task<ActionResult> RemovePermissionFromRole([FromBody] RoleModulePermissionDTO request)
        {
            var context = _contextAccessor.TenantDbContext;
            var role = await context.Roles
                .Include(r => r.AllowedActions)
                .FirstOrDefaultAsync(r => r.Id == request.RoleId);

            if (role == null)
                return NotFound("Role not found.");

            var moduleAction = await context.ModuleActions
                .FirstOrDefaultAsync(ma => ma.Id == request.ActionId && ma.ModuleId == request.ModuleId);

            if (moduleAction == null)
                return NotFound("Module action not found.");

            var permissionToRemove = role.AllowedActions
                .FirstOrDefault(a => a.Id == request.ActionId);

            if (permissionToRemove == null)
                return NotFound("Permission not found for this role.");

            role.AllowedActions.Remove(permissionToRemove);
            await context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("GetRoles")]
        public async Task<ActionResult<IEnumerable<RoleDTO>>> GetRoles()
        {
            var context = _contextAccessor.TenantDbContext;
            var roles = await context
                .Roles.Include(r => r.AllowedActions)
                .ThenInclude(rmp => rmp.Module)
                .Include(r => r.AllowedActions)
                .ThenInclude(rmp => rmp.Module)
                .ToListAsync();

            var roleDTOs = roles
                .Select(r => new RoleDTO
                {
                    Id = r.Id,
                    Name = r.Name
                })
                .ToList();

            return Ok(roleDTOs);
        }

        // Get role by ID
        [HttpGet("GetRole/{roleId}")]
        public async Task<ActionResult<RoleDTO>> GetRole(int roleId)
        {
            var context = _contextAccessor.TenantDbContext;
            var role = await context
                .Roles.Include(r => r.AllowedActions)
                .ThenInclude(rmp => rmp.Module)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return NotFound("Role not found.");

            var roleDTO = new RoleDTO
            {
                Id = role.Id,
                Name = role.Name,
                RoleModulePermissions = role.AllowedActions.Select(action => new RoleModulePermissionDTO
                {
                    RoleId = role.Id,
                    ModuleId = action.ModuleId,
                    ActionId = action.Id,
                    //ModuleName = action.Module.Name
                }).ToList()
            };

            return Ok(roleDTO);
        }

        // Assign role to a user
        [HttpPost("AssignRoleToUser")]
        public async Task<ActionResult<UserRoleDTO>> AssignRoleToUser(
            [FromBody] AssignRoleToUserRequest request
        )
        {
            var context = _contextAccessor.TenantDbContext;
            var user = await context
                .Users.Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
                return NotFound("User not found.");

            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
            if (role == null)
                return NotFound("Role not found.");

            // Check if the user already has the role
            if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
                return BadRequest("User already has this role.");

            // Assign the role to the user
            var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };

            user.UserRoles.Add(userRole);
            await context.SaveChangesAsync();

            return Ok(
                new UserRoleDTO
                {
                    UserId = userRole.UserId,
                    RoleId = userRole.RoleId,
                    RoleName = role.Name
                }
            );
        }

        // Remove role from a user
        [HttpDelete("RemoveRoleFromUser")]
        public async Task<ActionResult> RemoveRoleFromUser(
            [FromBody] RemoveRoleFromUserRequest request
        )
        {
            var context = _contextAccessor.TenantDbContext;
            var userRole = await context.UserRoles.FirstOrDefaultAsync(ur =>
                ur.UserId == request.UserId && ur.RoleId == request.RoleId
            );
            if (userRole == null)
                return NotFound("Role not assigned to user.");

            context.UserRoles.Remove(userRole);
            await context.SaveChangesAsync();

            return Ok();
        }

        private IEnumerable<string> GetActionsForModule(ModuleName module)
        {
            return module switch
            {
                ModuleName.UserManagement => Enum.GetNames(typeof(ModuleActions.UserManagement)),
                ModuleName.CommunityManagement
                    => Enum.GetNames(typeof(ModuleActions.CommunityManagement)),
                _ => Enumerable.Empty<string>(),
            };
        }

        [HttpGet("UserAccess/{userId}")]
       // [Permission(nameof(ModuleActions.UserManagement.Read))]
        public async Task<ActionResult<UserAccessDTO>> GetUserAccess(Guid userId)
        {
            var context = _contextAccessor.TenantDbContext;
            var user = await context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.AllowedActions)
                            .ThenInclude(a => a.Module)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found");

            var userAccessDTO = new UserAccessDTO
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = user.UserRoles.Select(ur => new UserRoleAccessDTO
                {
                    RoleId = ur.Role.Id,
                    RoleName = ur.Role.Name,
                    Permissions = ur.Role.AllowedActions.Select(aa => new RoleModulePermissionDTO
                    {
                        RoleId = ur.Role.Id,
                        ModuleId = aa.ModuleId,
                        ActionId = aa.Id
                    }).ToList()
                }).ToList()
            };

            return Ok(userAccessDTO);
        }
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; }
    }

    public class AssignPermissionRequest
    {
        public int RoleId { get; set; }
        public ModuleName Module { get; set; }
        public string Action { get; set; }
    }

    public class RemovePermissionRequest
    {
        public int RoleId { get; set; }
        public ModuleName Module { get; set; }
        public string Action { get; set; }
    }

    public class AssignRoleToUserRequest
    {
        public Guid UserId { get; set; }
        public int RoleId { get; set; }
    }

    public class RemoveRoleFromUserRequest
    {
        public Guid UserId { get; set; }
        public int RoleId { get; set; }
    }
}
