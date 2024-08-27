using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace starterapi;

public static class DbSeeder
{
    public static void SeedTenantData(TenantDbContext context)
    {

        using (var transaction = context.Database.BeginTransaction())
        {
            try
            {
                 

                // Ensure the database is created
                context.Database.EnsureCreated();

                // Seed Roles
                SeedRoles(context);

                // Seed Modules and Permissions
                SeedModulesAndPermissions(context);

                // Seed Super Admin User
                SeedSuperAdminUser(context);

                // Commit transaction if all seeding operations are successful
                transaction.Commit();
            }
            catch (Exception ex)
            {
                // Rollback transaction if any seeding operation fails
                transaction.Rollback();
                Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
                throw;
            }
        }
    }

    private static void SeedRoles(TenantDbContext context)
    {
        string[] roleNames = { "Super Admin", "Basic User" };

        foreach (var roleName in roleNames)
        {
            if (!context.Roles.Any(r => r.Name == roleName))
            {
                context.Roles.Add(new Role { Name = roleName });
            }
        }

        context.SaveChanges();
    }

    private static void SeedModulesAndPermissions(TenantDbContext context)
    {
        var controllers = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

        var superAdminRole = context.Roles.First(r => r.Name == "Super Admin");

        foreach (var controller in controllers)
        {
            var moduleAttribute = controller.GetCustomAttribute<ModuleAttribute>();
            if (moduleAttribute != null)
            {
                var module = context.Modules.FirstOrDefault(m => m.Name == moduleAttribute.Name);
                if (module == null)
                {
                    module = new Module { Name = moduleAttribute.Name };
                    context.Modules.Add(module);
                    context.SaveChanges();
                }

                var actions = controller
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.IsDefined(typeof(PermissionAttribute), false));

                foreach (var action in actions)
                {
                    var permissionAttribute = action.GetCustomAttribute<PermissionAttribute>();
                    if (permissionAttribute != null)
                    {
                        var permission = permissionAttribute.Name;

                        // Check if the permission already exists
                        var existingPermission = context
                            .RoleModulePermissions.AsNoTracking()
                            .FirstOrDefault(rmp =>
                                rmp.RoleId == superAdminRole.Id
                                && rmp.ModuleId == module.Id
                                && rmp.Permission == permission
                            );

                        if (existingPermission == null)
                        {
                            context.RoleModulePermissions.Add(
                                new RoleModulePermission
                                {
                                    RoleId = superAdminRole.Id,
                                    ModuleId = module.Id,
                                    Permission = permission
                                }
                            );
                            context.SaveChanges();
                        }
                    }
                }
            }
        }
    }

    private static void SeedSuperAdminUser(TenantDbContext context)
    {
        if (!context.Users.Any(u => u.Email == "superadmin@example.com"))
        {
            var superAdminRole = context.Roles.First(r => r.Name == "Super Admin");

            var superAdminUser = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = "superadmin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdminPassword123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(superAdminUser);
            context.SaveChanges();

            context.UserRoles.Add(
                new UserRole { UserId = superAdminUser.Id, RoleId = superAdminRole.Id }
            );

            context.SaveChanges();
        }
    }
}
