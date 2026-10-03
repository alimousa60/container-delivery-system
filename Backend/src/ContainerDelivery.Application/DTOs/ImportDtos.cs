using System.ComponentModel.DataAnnotations;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Application.DTOs;

public class ImportResultDto
{
    public int ImportBatchId { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int DuplicateVins { get; set; }
    public int ContainersCreated { get; set; }
    public int VehiclesImported { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = [];
    public TimeSpan ProcessingTime { get; set; }
}

public class ImportBatchStatusDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public ImportStatus Status { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = [];
}

public class ImportErrorDto
{
    public int Row { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}