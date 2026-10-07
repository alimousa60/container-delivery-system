using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class User : BaseEntity
{
    public User()
    {
    }

    public User(string email, string passwordHash, string fullName)
    {
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
    }

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
    public virtual ICollection<Container> ClosedContainers { get; set; } = new List<Container>();
    public virtual ICollection<Vehicle> DeliveredVehicles { get; set; } = new List<Vehicle>();
    public virtual ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<ContainerReport> GeneratedReports { get; set; } = new List<ContainerReport>();
    public virtual ICollection<ImportBatch> ImportedBatches { get; set; } = new List<ImportBatch>();

    // Helper properties
    [NotMapped]
    public IEnumerable<ContainerDelivery.Core.Enums.UserRole> Roles => UserRoles.Select(ur => ur.Role);

    [NotMapped]
    public bool IsAdmin => UserRoles.Any(ur => ur.Role == ContainerDelivery.Core.Enums.UserRole.Admin);

    [NotMapped]
    public bool IsDeliveryUser => UserRoles.Any(ur => ur.Role == ContainerDelivery.Core.Enums.UserRole.DeliveryUser);

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void EnableMfa(string secret, string recoveryCodes)
    {
        MfaSecret = secret;
        MfaRecoveryCodes = recoveryCodes;
        IsMfaEnabled = true;
    }

    public void DisableMfa()
    {
        MfaSecret = null;
        MfaRecoveryCodes = null;
        IsMfaEnabled = false;
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