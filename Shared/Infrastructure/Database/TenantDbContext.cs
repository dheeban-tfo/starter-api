using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using starterapi.Models;
using StarterApi.Models;
using starterapi.Services;

namespace starterapi;

public class TenantDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // public TenantDbContext(
    //     DbContextOptions<TenantDbContext> options,
    //     IHttpContextAccessor httpContextAccessor
    // )
    //     : base(options)
    // {
    //     _httpContextAccessor = httpContextAccessor;
    // }

    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Module> Modules { get; set; }

    public DbSet<ModuleAction> ModuleActions { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public DbSet<Community> Communities { get; set; }
    public DbSet<Block> Blocks { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<UnitOwnership> UnitOwnerships { get; set; }
    public DbSet<UnitResident> UnitResidents { get; set; }

    public DbSet<Product> Products { get; set; }

     public DbSet<Facility> Facilities { get; set; }
    public DbSet<FacilityBooking> FacilityBookings { get; set; }

    public DbSet<RegisteredDevice> RegisteredDevices { get; set; }
    public DbSet<DeviceSubscription> DeviceSubscriptions { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (
                entry.Entity is AuditLog
                || entry.State == EntityState.Detached
                || entry.State == EntityState.Unchanged
            )
                continue;

            var auditEntry = new AuditEntry(entry);
            auditEntry.TableName = entry.Entity.GetType().Name;
            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        return auditEntries;
    }

    private Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            var userId = "System"; // _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            AuditLogs.Add(
                new AuditLog
                {
                    UserId = userId,
                    Action = auditEntry.Action ?? "Unknown",
                    EntityName = auditEntry.TableName,
                    EntityId = JsonConvert.SerializeObject(auditEntry.KeyValues),
                    OldValues = JsonConvert.SerializeObject(auditEntry.OldValues),
                    NewValues = JsonConvert.SerializeObject(auditEntry.NewValues),
                    Timestamp = DateTime.UtcNow
                }
            );
        }

        return SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Many-to-Many for User and Role
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder
            .Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder
            .Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        // Configure ModuleAction
        modelBuilder
            .Entity<ModuleAction>()
            .HasOne(ma => ma.Module)
            .WithMany(m => m.Actions)
            .HasForeignKey(ma => ma.ModuleId);

        // Configure Role and ModuleAction many-to-many relationship
        modelBuilder
            .Entity<Role>()
            .HasMany(r => r.AllowedActions)
            .WithMany(ma => ma.Roles)
            .UsingEntity(j => j.ToTable("RoleModuleActions"));

        // Configure UnitOwnership
        modelBuilder
            .Entity<UnitOwnership>()
            .HasKey(uo => new
            {
                uo.UnitId,
                uo.UserId,
                uo.OwnershipStartDate
            });

        // Configure UnitResident
        modelBuilder
            .Entity<UnitResident>()
            .HasKey(ur => new
            {
                ur.UnitId,
                ur.UserId,
                ur.MoveInDate
            });

        // Configure Product Price
        modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");

         modelBuilder.Entity<Facility>()
                .HasOne(f => f.Community)
                .WithMany()
                .HasForeignKey(f => f.CommunityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FacilityBooking>()
                .HasOne(fb => fb.Facility)
                .WithMany(f => f.Bookings)
                .HasForeignKey(fb => fb.FacilityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FacilityBooking>()
                .HasOne(fb => fb.User)
                .WithMany()
                .HasForeignKey(fb => fb.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        // You may want to add other configurations here if needed, such as:
        // - Configuring indexes
        // - Setting up cascade delete behaviors
        // - Configuring any other entity relationships not covered above
    }
}
