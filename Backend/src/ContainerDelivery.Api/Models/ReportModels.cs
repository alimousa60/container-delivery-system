using System.ComponentModel.DataAnnotations;

namespace ContainerDelivery.Api.Models;

public record ContainerReportDto
{
    public int Id { get; init; }
    public int ContainerId { get; init; }
    public string ContainerNumber { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public string GeneratedBy { get; init; } = string.Empty;
    public int TotalVehicles { get; init; }
    public int DeliveredVehicles { get; init; }
    public int UndeliveredVehicles { get; init; }
    public decimal CompletionPercentage { get; init; }
}

public record BulkReportRequest
{
    [Required(ErrorMessage = "At least one container ID is required")]
    [MinLength(1, ErrorMessage = "At least one container ID is required")]
    public List<int> ContainerIds { get; init; } = [];
}

public record BulkReportResponse
{
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<BulkReportErrorDto> Errors { get; init; } = [];
}

public record BulkReportErrorDto
{
    public int ContainerId { get; init; }
    public string Error { get; init; } = string.Empty;
}