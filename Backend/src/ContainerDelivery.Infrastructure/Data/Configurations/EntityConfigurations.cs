using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ContainerDelivery.Core.Entities;

namespace ContainerDelivery.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.MfaSecret).HasMaxLength(32);
        builder.Property(u => u.MfaRecoveryCodes).HasColumnType("nvarchar(max)");
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.IsMfaEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.FailedLoginAttempts).IsRequired().HasDefaultValue(0);

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Property(ur => ur.AssignedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(ur => ur.AssignedByUserId).IsRequired();

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.AssignedByUser)
            .WithMany()
            .HasForeignKey(ur => ur.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("Containers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ContainerNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.ContainerNumber).IsUnique();
        builder.Property(c => c.TotalVehicles).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.DeliveredVehicles).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20).HasDefaultValue("NotStarted");
        builder.Property(c => c.CreatedByUserId).IsRequired();
        builder.Property(c => c.Notes).HasColumnType("nvarchar(max)");

        builder.HasOne(c => c.CreatedByUser)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Vehicles)
            .WithOne(v => v.Container)
            .HasForeignKey(v => v.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.DeliveryRecords)
            .WithOne(dr => dr.Container)
            .HasForeignKey(dr => dr.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Reports)
            .WithOne(r => r.Container)
            .HasForeignKey(r => r.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Vin).IsRequired().HasMaxLength(50);
        builder.HasIndex(v => v.Vin).IsUnique();
        builder.Property(v => v.Description).IsRequired().HasMaxLength(500);
        builder.Property(v => v.ContainerId).IsRequired();
        builder.Property(v => v.IsDelivered).IsRequired().HasDefaultValue(false);
        builder.HasIndex(v => v.IsDelivered);

        builder.HasOne(v => v.Container)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.DeliveredByUser)
            .WithMany()
            .HasForeignKey(v => v.DeliveredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(v => v.DeliveryRecords)
            .WithOne(dr => dr.Vehicle)
            .HasForeignKey(dr => dr.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeliveryRecordConfiguration : IEntityTypeConfiguration<DeliveryRecord>
{
    public void Configure(EntityTypeBuilder<DeliveryRecord> builder)
    {
        builder.ToTable("DeliveryRecords");
        builder.HasKey(dr => dr.Id);
        builder.Property(dr => dr.VehicleId).IsRequired();
        builder.Property(dr => dr.ContainerId).IsRequired();
        builder.Property(dr => dr.DeliveredByUserId).IsRequired();
        builder.Property(dr => dr.DeliveryMethod).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(dr => dr.Notes).HasColumnType("nvarchar(max)");
        builder.Property(dr => dr.ScannedVin).HasMaxLength(50);

        builder.HasOne(dr => dr.Vehicle)
            .WithMany(v => v.DeliveryRecords)
            .HasForeignKey(dr => dr.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dr => dr.Container)
            .WithMany(c => c.DeliveryRecords)
            .HasForeignKey(dr => dr.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dr => dr.DeliveredByUser)
            .WithMany()
            .HasForeignKey(dr => dr.DeliveredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserId).IsRequired(false);
        builder.Property(a => a.Action).IsRequired().HasConversion<string>().HasMaxLength(100);
        builder.Property(a => a.EntityType).IsRequired().HasConversion<string>().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired(false);
        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.Action);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ContainerReportConfiguration : IEntityTypeConfiguration<ContainerReport>
{
    public void Configure(EntityTypeBuilder<ContainerReport> builder)
    {
        builder.ToTable("ContainerReports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ContainerId).IsRequired();
        builder.Property(r => r.GeneratedByUserId).IsRequired();
        builder.Property(r => r.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(r => r.FileName).IsRequired().HasMaxLength(255);
        builder.Property(r => r.TotalVehicles).IsRequired();
        builder.Property(r => r.DeliveredVehicles).IsRequired();
        builder.Property(r => r.UndeliveredVehicles).IsRequired();
        builder.Property(r => r.CompletionPercentage).IsRequired().HasPrecision(5, 2);

        builder.HasOne(r => r.Container)
            .WithMany(c => c.Reports)
            .HasForeignKey(r => r.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.GeneratedByUser)
            .WithMany()
            .HasForeignKey(r => r.GeneratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FileName).IsRequired().HasMaxLength(255);
        builder.Property(i => i.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(i => i.TotalRecords).IsRequired().HasDefaultValue(0);
        builder.Property(i => i.SuccessfulRecords).IsRequired().HasDefaultValue(0);
        builder.Property(i => i.FailedRecords).IsRequired().HasDefaultValue(0);
        builder.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(20).HasDefaultValue("Pending");
        builder.Property(i => i.ErrorDetails).HasColumnType("nvarchar(max)");
        builder.Property(i => i.ImportedByUserId).IsRequired();

        builder.HasOne(i => i.ImportedByUser)
            .WithMany()
            .HasForeignKey(i => i.ImportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}