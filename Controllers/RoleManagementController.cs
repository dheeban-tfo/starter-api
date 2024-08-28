using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi.Services;
using starterapi.DTOs;

namespace starterapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleManagementController : ControllerBase
    {
        private readonly ITenantDbContextAccessor _contextAccessor;
        private readonly ILogger<RoleManagementController> _logger;

        public RoleManagementController(ITenantDbContextAccessor contextAccessor, ILogger<RoleManagementController> logger)
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

        // Assign permission to role
        [HttpPost("AssignPermissionToRole")]
        public async Task<ActionResult<RoleModulePermissionDTO>> AssignPermissionToRole([FromBody] AssignPermissionRequest request)
        {
            var context = _contextAccessor.TenantDbContext;
            var role = await context.Roles.FindAsync(request.RoleId);
            if (role == null)
                return NotFound("Role not found.");

            var module = await context.Modules.FindAsync(request.ModuleId);
            if (module == null)
                return NotFound("Module not found.");

            var permissionExists = await context.RoleModulePermissions.AnyAsync(rmp =>
                rmp.RoleId == role.Id
                && rmp.ModuleId == module.Id
                && rmp.Permission == request.Permission
            );

            if (permissionExists)
                return BadRequest("Permission already assigned to this role.");

            var roleModulePermission = new RoleModulePermission
            {
                RoleId = role.Id,
                ModuleId = module.Id,
                Permission = request.Permission
            };

            context.RoleModulePermissions.Add(roleModulePermission);
            await context.SaveChangesAsync();

            return Ok(new RoleModulePermissionDTO
            {
                RoleId = roleModulePermission.RoleId,
                ModuleId = roleModulePermission.ModuleId,
                Permission = roleModulePermission.Permission,
                ModuleName = module.Name
            });
        }

        // Remove permission from role
        [HttpDelete("RemovePermissionFromRole")]
        public async Task<ActionResult> RemovePermissionFromRole([FromBody] RemovePermissionRequest request)
        {
            var context = _contextAccessor.TenantDbContext;
            var roleModulePermission = await context.RoleModulePermissions.FirstOrDefaultAsync(rmp =>
                rmp.RoleId == request.RoleId
                && rmp.ModuleId == request.ModuleId
                && rmp.Permission == request.Permission
            );

            if (roleModulePermission == null)
                return NotFound("Permission not found for this role.");

            context.RoleModulePermissions.Remove(roleModulePermission);
            await context.SaveChangesAsync();

            return Ok();
        }

        // Get all roles
        [HttpGet("GetRoles")]
        public async Task<ActionResult<IEnumerable<RoleDTO>>> GetRoles()
        {
            var context = _contextAccessor.TenantDbContext;
            var roles = await context.Roles
                .Include(r => r.RoleModulePermissions)
                .ThenInclude(rmp => rmp.Module)
                .ToListAsync();

            var roleDTOs = roles.Select(r => new RoleDTO
            {
                Id = r.Id,
                Name = r.Name,
                RoleModulePermissions = r.RoleModulePermissions.Select(rmp => new RoleModulePermissionDTO
                {
                    RoleId = rmp.RoleId,
                    ModuleId = rmp.ModuleId,
                    Permission = rmp.Permission,
                    ModuleName = rmp.Module.Name
                }).ToList()
            }).ToList();

            return Ok(roleDTOs);
        }

        // Get role by ID
        [HttpGet("GetRole/{roleId}")]
        public async Task<ActionResult<RoleDTO>> GetRole(int roleId)
        {
            var context = _contextAccessor.TenantDbContext;
            var role = await context.Roles
                .Include(r => r.RoleModulePermissions)
                .ThenInclude(rmp => rmp.Module)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return NotFound("Role not found.");

            var roleDTO = new RoleDTO
            {
                Id = role.Id,
                Name = role.Name,
                RoleModulePermissions = role.RoleModulePermissions.Select(rmp => new RoleModulePermissionDTO
                {
                    RoleId = rmp.RoleId,
                    ModuleId = rmp.ModuleId,
                    Permission = rmp.Permission,
                    ModuleName = rmp.Module.Name
                }).ToList()
            };

            return Ok(roleDTO);
        }

        // Assign role to a user
        [HttpPost("AssignRoleToUser")]
        public async Task<ActionResult<UserRoleDTO>> AssignRoleToUser([FromBody] AssignRoleToUserRequest request)
        {
            var context = _contextAccessor.TenantDbContext;
            var user = await context.Users.Include(u => u.UserRoles)
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

            return Ok(new UserRoleDTO
            {
                UserId = userRole.UserId,
                RoleId = userRole.RoleId,
                RoleName = role.Name
            });
        }

        // Remove role from a user
        [HttpDelete("RemoveRoleFromUser")]
        public async Task<ActionResult> RemoveRoleFromUser([FromBody] RemoveRoleFromUserRequest request)
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
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; }
    }

    public class AssignPermissionRequest
    {
        public int RoleId { get; set; }
        public int ModuleId { get; set; }
        public string Permission { get; set; }
    }

    public class RemovePermissionRequest
    {
        public int RoleId { get; set; }
        public int ModuleId { get; set; }
        public string Permission { get; set; }
    }

    public class AssignRoleToUserRequest
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }

    public class RemoveRoleFromUserRequest
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }
}