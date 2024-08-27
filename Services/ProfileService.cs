using Microsoft.EntityFrameworkCore;
using starterapi.Models;

namespace starterapi.Services;

public interface IProfileService
{
    Task<UserProfileResponse> GetUserProfileAsync(User user);
}

public class ProfileService : IProfileService
{
    private readonly TenantDbContext _context;

    public ProfileService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(User user)
    {
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RoleModulePermissions)
            .ThenInclude(rmp => rmp.Module)
            .ToListAsync();

        var roles = userRoles.Select(ur => ur.Role.Name).ToList();

        var modulePermissions = userRoles
            .SelectMany(ur => ur.Role.RoleModulePermissions)
            .GroupBy(rmp => rmp.Module.Name)
            .ToDictionary(
                g => g.Key,
                g => g.Select(rmp => rmp.Permission).Distinct().ToList()
            );

        return new UserProfileResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = roles,
            ModulePermissions = modulePermissions
        };
    }
}