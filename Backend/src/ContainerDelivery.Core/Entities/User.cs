using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class User : BaseEntity
{
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }

    public bool IsMfaEnabled { get; set; } = false;

    [MaxLength(32)]
    public string? MfaSecret { get; set; }

    public string? MfaRecoveryCodes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockedOutUntil { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Container> CreatedContainers { get; set; } = new List<Container>();
    public virtual ICollection<Vehicle> DeliveredVehicles { get; set; } = new List<Vehicle>();
    public virtual ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<ContainerReport> GeneratedReports { get; set; } = new List<ContainerReport>();
    public virtual ICollection<ImportBatch> ImportedBatches { get; set; } = new List<ImportBatch>();

    // Helper properties
    [NotMapped]
    public IEnumerable<UserRole> Roles => UserRoles.Select(ur => ur.Role);

    [NotMapped]
    public bool IsAdmin => UserRoles.Any(ur => ur.Role == UserRole.Admin);

    [NotMapped]
    public bool IsDeliveryUser => UserRoles.Any(ur => ur.Role == UserRole.DeliveryUser);

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            LockedOutUntil = DateTime.UtcNow.AddMinutes(15);
        }
    }

    public bool IsLockedOut => LockedOutUntil.HasValue && LockedOutUntil.Value > DateTime.UtcNow;
}