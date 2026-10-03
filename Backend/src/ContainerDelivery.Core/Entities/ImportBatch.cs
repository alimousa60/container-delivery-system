using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class ImportBatch : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public int TotalRecords { get; set; } = 0;

    public int SuccessfulRecords { get; set; } = 0;

    public int FailedRecords { get; set; } = 0;

    [Required]
    public ImportStatus Status { get; set; } = ImportStatus.Pending;

    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorDetails { get; set; }

    [Required]
    public int ImportedByUserId { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(ImportedByUserId))]
    public virtual User ImportedByUser { get; set; } = null!;

    public void StartProcessing()
    {
        Status = ImportStatus.Processing;
        SetUpdatedAt();
    }

    public void Complete(int successful, int failed, string? errors = null)
    {
        SuccessfulRecords = successful;
        FailedRecords = failed;
        TotalRecords = successful + failed;
        Status = failed > 0 && successful == 0 ? ImportStatus.Failed : ImportStatus.Completed;
        ErrorDetails = errors;
        CompletedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Fail(string error)
    {
        Status = ImportStatus.Failed;
        ErrorDetails = error;
        CompletedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}