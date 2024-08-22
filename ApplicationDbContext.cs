using Microsoft.EntityFrameworkCore;

namespace starterapi;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
     public DbSet<Role> Roles { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<RoleModulePermission> RoleModulePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure entity settings here if needed

        // Many-to-Many for User and Role
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        // Many-to-Many for Role and Module with Permission
        modelBuilder.Entity<RoleModulePermission>()
            .HasKey(rm => new { rm.RoleId, rm.ModuleId, rm.Permission });

        modelBuilder.Entity<RoleModulePermission>()
            .HasOne(rm => rm.Role)
            .WithMany(r => r.RoleModulePermissions)
            .HasForeignKey(rm => rm.RoleId);

        modelBuilder.Entity<RoleModulePermission>()
            .HasOne(rm => rm.Module)
            .WithMany(m => m.RoleModulePermissions)
            .HasForeignKey(rm => rm.ModuleId);
    }
    
}

