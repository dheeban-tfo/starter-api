using Microsoft.EntityFrameworkCore;
using starterapi.Models;

namespace starterapi.Services;

public interface IProfileService
{
    Task<UserProfileResponse> GetUserProfileAsync(User user);
}

public class ProfileService : IProfileService
{
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(ITenantDbContextAccessor contextAccessor, ILogger<ProfileService> logger)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

     public async Task<UserProfileResponse> GetUserProfileAsync(User user)
    {
        _logger.LogInformation($"Getting profile for user: {user.Id}");

        var context = _contextAccessor.TenantDbContext;

        var userRoles = await context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .ThenInclude(rmp => rmp.AllowedActions)
            .ToListAsync();

        var roles = userRoles.Select(ur => ur.Role.Name).ToList();

        var modulePermissions = userRoles
            .SelectMany(ur => ur.Role.AllowedActions)
            .GroupBy(rmp => rmp.Module.Name)
            .ToDictionary(
                g => g.Key,
                g => g.Select(rmp => rmp.Name).Distinct().ToList()
            );

        _logger.LogInformation($"Profile retrieved for user: {user.Id}. Roles: {string.Join(", ", roles)}");

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