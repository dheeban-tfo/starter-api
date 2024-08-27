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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Identifier)
            .IsUnique();
    }
}