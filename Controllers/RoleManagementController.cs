using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace starterapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoleManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create a new role
        [HttpPost("CreateRole")]
        public async Task<ActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (await _context.Roles.AnyAsync(r => r.Name == request.Name))
            {
                return BadRequest("Role already exists.");
            }

            var role = new Role { Name = request.Name };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(role);
        }

        // Assign permission to role
        [HttpPost("AssignPermissionToRole")]
        public async Task<ActionResult> AssignPermissionToRole(
            [FromBody] AssignPermissionRequest request
        )
        {
            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
                return NotFound("Role not found.");

            var module = await _context.Modules.FindAsync(request.ModuleId);
            if (module == null)
                return NotFound("Module not found.");

            var permissionExists = await _context.RoleModulePermissions.AnyAsync(rmp =>
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

            _context.RoleModulePermissions.Add(roleModulePermission);
            await _context.SaveChangesAsync();

            return Ok(roleModulePermission);
        }

        // Remove permission from role
        [HttpDelete("RemovePermissionFromRole")]
        public async Task<ActionResult> RemovePermissionFromRole(
            [FromBody] RemovePermissionRequest request
        )
        {
            var roleModulePermission = await _context.RoleModulePermissions.FirstOrDefaultAsync(
                rmp =>
                    rmp.RoleId == request.RoleId
                    && rmp.ModuleId == request.ModuleId
                    && rmp.Permission == request.Permission
            );

            if (roleModulePermission == null)
                return NotFound("Permission not found for this role.");

            _context.RoleModulePermissions.Remove(roleModulePermission);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Get all roles
        [HttpGet("GetRoles")]
        public async Task<ActionResult> GetRoles()
        {
            var roles = await _context.Roles.Include(r => r.RoleModulePermissions).ToListAsync();
            return Ok(roles);
        }

        // Get role by ID
        [HttpGet("GetRole/{roleId}")]
        public async Task<ActionResult> GetRole(int roleId)
        {
            var role = await _context
                .Roles.Include(r => r.RoleModulePermissions)
                .ThenInclude(rmp => rmp.Module)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return NotFound("Role not found.");

            return Ok(role);
        }

        // Assign role to a user
        [HttpPost("AssignRoleToUser")]
        public async Task<ActionResult> AssignRoleToUser([FromBody] AssignRoleToUserRequest request)
        {
            var user = await _context
                .Users.Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
                return NotFound("User not found.");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
            if (role == null)
                return NotFound("Role not found.");

            // Check if the user already has the role
            if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
                return BadRequest("User already has this role.");

            // Assign the role to the user
            var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };

            user.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Ok(userRole);
        }

        // Remove role from a user
        [HttpDelete("RemoveRoleFromUser")]
        public async Task<ActionResult> RemoveRoleFromUser(
            [FromBody] RemoveRoleFromUserRequest request
        )
        {
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur =>
                ur.UserId == request.UserId && ur.RoleId == request.RoleId
            );
            if (userRole == null)
                return NotFound("Role not assigned to user.");

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok();
        }
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
