using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContainerDelivery.Core.Entities;

public class ContainerReport : BaseEntity
{
    [Required]
    public int ContainerId { get; set; }

    [Required]
    public int GeneratedByUserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    public int TotalVehicles { get; set; }

    public int DeliveredVehicles { get; set; }

    public int UndeliveredVehicles { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CompletionPercentage { get; set; }

    public long FileSizeBytes { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(ContainerId))]
    public virtual Container Container { get; set; } = null!;

    [ForeignKey(nameof(GeneratedByUserId))]
    public virtual User GeneratedByUser { get; set; } = null!;
}