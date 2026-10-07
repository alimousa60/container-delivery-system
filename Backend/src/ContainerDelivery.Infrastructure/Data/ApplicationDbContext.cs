using Microsoft.EntityFrameworkCore;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using UserRole = ContainerDelivery.Core.Entities.UserRole;
using UserRoleEnum = ContainerDelivery.Core.Enums.UserRole;

namespace ContainerDelivery.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<DeliveryRecord> DeliveryRecords => Set<DeliveryRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ContainerReport> ContainerReports => Set<ContainerReport>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure decimal precision
        modelBuilder.Entity<ContainerReport>()
            .Property(r => r.CompletionPercentage)
            .HasPrecision(5, 2);

        // Configure indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.Vin)
            .IsUnique();

        modelBuilder.Entity<Container>()
            .HasIndex(c => c.ContainerNumber)
            .IsUnique();

        // Configure CompanySettings
        modelBuilder.Entity<CompanySettings>()
            .HasIndex(cs => cs.IsActive)
            .HasFilter("[IsActive] = 1");

        // Seed default company settings
        modelBuilder.Entity<CompanySettings>().HasData(
            new CompanySettings
            {
                Id = 1,
                CompanyName = "Container Delivery System",
                Address = "123 Shipping Lane, Port City, PC 12345",
                Phone = "+1 (555) 123-4567",
                Email = "info@container-delivery.com",
                ReportFooter = "This document is confidential and intended solely for the use of the individual or entity to whom it is addressed.",
                Website = "https://container-delivery.com",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Configure cascading deletes
        modelBuilder.Entity<Container>()
            .HasMany(c => c.Vehicles)
            .WithOne(v => v.Container)
            .HasForeignKey(v => v.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Container>()
            .HasMany(c => c.DeliveryRecords)
            .WithOne(dr => dr.Container)
            .HasForeignKey(dr => dr.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vehicle>()
            .HasMany(v => v.DeliveryRecords)
            .WithOne(dr => dr.Vehicle)
            .HasForeignKey(dr => dr.VehicleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Container>()
            .HasMany(c => c.Reports)
            .WithOne(r => r.Container)
            .HasForeignKey(r => r.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.AssignedByUser)
            .WithMany()
            .HasForeignKey(ur => ur.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ContainerReport>()
            .HasOne(r => r.GeneratedByUser)
            .WithMany()
            .HasForeignKey(r => r.GeneratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImportBatch>()
            .HasOne(b => b.ImportedByUser)
            .WithMany()
            .HasForeignKey(b => b.ImportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Container>()
            .HasOne(c => c.CreatedByUser)
            .WithMany(u => u.CreatedContainers)
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Container>()
            .HasOne(c => c.ClosedByUser)
            .WithMany(u => u.ClosedContainers)
            .HasForeignKey(c => c.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = UserRoleEnum.Admin, Description = "Full system access including user management, audit logs, and container deletion" },
            new Role { Id = 2, Name = UserRoleEnum.DeliveryUser, Description = "Can view assigned containers, confirm deliveries, and generate reports" }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdatedAt();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

// Add Role entity for seeding
public class Role
{
    public int Id { get; set; }
    public UserRoleEnum Name { get; set; }
    public string? Description { get; set; }
}