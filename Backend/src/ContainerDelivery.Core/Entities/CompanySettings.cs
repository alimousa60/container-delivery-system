using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContainerDelivery.Core.Entities;

namespace ContainerDelivery.Core.Entities;

public class CompanySettings : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = "Container Delivery System";

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(2000)]
    public string? ReportFooter { get; set; }

    [MaxLength(100)]
    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastModifiedAt { get; set; }

    public int? LastModifiedByUserId { get; set; }

    [ForeignKey(nameof(LastModifiedByUserId))]
    public virtual User? LastModifiedByUser { get; set; }
}