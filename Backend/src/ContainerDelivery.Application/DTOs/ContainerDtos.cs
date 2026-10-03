using System.ComponentModel.DataAnnotations;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Application.DTOs;

public class ContainerDto
{
    public int Id { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public int TotalVehicles { get; set; }
    public int DeliveredVehicles { get; set; }
    public ContainerStatus Status { get; set; }
    public double CompletionPercentage { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ContainerDetailDto : ContainerDto
{
    public string? Notes { get; set; }
    public List<VehicleDto> Vehicles { get; set; } = [];
}

public class VehicleDto
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveredBy { get; set; }
}

public class CreateContainerRequest
{
    [Required(ErrorMessage = "Container number is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Container number must be between 1 and 50 characters")]
    public string ContainerNumber { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }
}

public class UpdateContainerRequest
{
    [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }
}

public class DashboardStatsDto
{
    public int TotalContainers { get; set; }
    public int TotalVehicles { get; set; }
    public int PendingContainers { get; set; }
    public int CompletedContainers { get; set; }
    public int InProgressContainers { get; set; }
    public int DeliveredVehicles { get; set; }
    public int UndeliveredVehicles { get; set; }
}