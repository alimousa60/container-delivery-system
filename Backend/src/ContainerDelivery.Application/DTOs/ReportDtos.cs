using System.ComponentModel.DataAnnotations;

namespace ContainerDelivery.Application.DTOs;

public class ContainerReportDto
{
    public int Id { get; set; }
    public int ContainerId { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public int TotalVehicles { get; set; }
    public int DeliveredVehicles { get; set; }
    public int UndeliveredVehicles { get; set; }
    public decimal CompletionPercentage { get; set; }
}

public class BulkReportRequest
{
    [Required(ErrorMessage = "At least one container ID is required")]
    [MinLength(1, ErrorMessage = "At least one container ID is required")]
    public List<int> ContainerIds { get; set; } = [];
}

public class BulkReportResponse
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkReportErrorDto> Errors { get; set; } = [];
}

public class BulkReportErrorDto
{
    public int ContainerId { get; set; }
    public string Error { get; set; } = string.Empty;
}