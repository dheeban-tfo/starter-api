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

                // Seed Products (if needed)
                SeedProducts(context);

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
    var superAdminRole = context.Roles.Include(r => r.AllowedActions).First(r => r.Name == "Super Admin");
    var modulesToAdd = new List<Module>();
    var actionsToAdd = new List<ModuleAction>();

    foreach (ModuleName moduleName in Enum.GetValues(typeof(ModuleName)))
    {
        var module = context.Modules.FirstOrDefault(m => m.Name == moduleName.ToString());
        if (module == null)
        {
            module = new Module { Name = moduleName.ToString() };
            modulesToAdd.Add(module);
        }

        var actions = GetActionsForModule(moduleName);

        foreach (var action in actions)
        {
            var moduleAction = context.ModuleActions.FirstOrDefault(ma =>
                ma.Module.Name == module.Name && ma.Name == action
            );
            if (moduleAction == null)
            {
                moduleAction = new ModuleAction { Module = module, Name = action };
                actionsToAdd.Add(moduleAction);
            }
        }
    }

    context.Modules.AddRange(modulesToAdd);
    context.ModuleActions.AddRange(actionsToAdd);
    context.SaveChanges();

    // Refresh the context to ensure we have up-to-date data
    context.ChangeTracker.Clear();
    superAdminRole = context.Roles.Include(r => r.AllowedActions).First(r => r.Name == "Super Admin");

    var allModuleActions = context.ModuleActions.ToList();
    foreach (var action in allModuleActions)
    {
        if (!superAdminRole.AllowedActions.Any(ma => ma.Id == action.Id))
        {
            superAdminRole.AllowedActions.Add(action);
        }
    }

    try
    {
        context.SaveChanges();
    }
    catch (DbUpdateException ex)
    {
        // Log the error
        Console.WriteLine($"Error occurred while saving changes: {ex.Message}");
        // Optionally, you can rethrow the exception if you want it to propagate
        // throw;
    }
}

    private static IEnumerable<string> GetActionsForModule(ModuleName module)
    {
        return module switch
        {
            ModuleName.UserManagement => Enum.GetNames(typeof(ModuleActions.UserManagement)),
            ModuleName.CommunityManagement
                => Enum.GetNames(typeof(ModuleActions.CommunityManagement)),
            _ => Enumerable.Empty<string>(),
        };
    }

    private static void SeedSuperAdminUser(TenantDbContext context)
    {
        if (!context.Users.Any(u => u.Email == "superadmin@example.com"))
        {
            var superAdminRole = context.Roles.First(r => r.Name == "Super Admin");

            var superAdminUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Super",
                LastName = "Admin",
                Email = "superadmin@example.com",
                PhoneNumber = "1234567890",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdminPassword123"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailVerified = true
            };

            context.Users.Add(superAdminUser);
            context.SaveChanges();

            context.UserRoles.Add(
                new UserRole { UserId = superAdminUser.Id, RoleId = superAdminRole.Id }
            );

            context.SaveChanges();
        }
    }

    private static void SeedProducts(TenantDbContext context)
    {
        if (!context.Products.Any())
        {
            context.Products.AddRange(
                new Product
                {
                    Name = "Product 1",
                    Price = 10.99m,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Product 2",
                    Price = 20.99m,
                    CreatedAt = DateTime.UtcNow
                }
            );
            context.SaveChanges();
        }
    }
}
