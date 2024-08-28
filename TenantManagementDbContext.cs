using Microsoft.EntityFrameworkCore;
using starterapi.Models;

namespace starterapi;

public class TenantManagementDbContext : DbContext
{
    public TenantManagementDbContext(DbContextOptions<TenantManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<RoleModulePermission> RoleModulePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Identifier)
            .IsUnique();

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

        modelBuilder.Entity<RoleModulePermission>()
            .HasKey(rmp => new { rmp.RoleId, rmp.ModuleId, rmp.Permission });

        modelBuilder.Entity<RoleModulePermission>()
            .HasOne(rmp => rmp.Role)
            .WithMany(r => r.RoleModulePermissions)
            .HasForeignKey(rmp => rmp.RoleId);

        modelBuilder.Entity<RoleModulePermission>()
            .HasOne(rmp => rmp.Module)
            .WithMany(m => m.RoleModulePermissions)
            .HasForeignKey(rmp => rmp.ModuleId);
    }
}