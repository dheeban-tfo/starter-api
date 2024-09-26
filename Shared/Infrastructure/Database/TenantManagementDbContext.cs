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
    public DbSet<ModuleAction> ModuleActions { get; set; }

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

        modelBuilder.Entity<ModuleAction>()
            .HasOne(ma => ma.Module)
            .WithMany(m => m.Actions)
            .HasForeignKey(ma => ma.ModuleId);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.AllowedActions)
            .WithMany(ma => ma.Roles)
            .UsingEntity(j => j.ToTable("RoleModuleActions"));
    }
}