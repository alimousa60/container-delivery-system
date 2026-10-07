using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Api.Models;

public record ContainerDto
{
    public int Id { get; init; }
    public string ContainerNumber { get; init; } = string.Empty;
    public int TotalVehicles { get; init; }
    public int DeliveredVehicles { get; init; }
    public ContainerStatus Status { get; init; }
    public double CompletionPercentage { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? ClosedBy { get; init; }
}

public record ContainerDetailDto : ContainerDto
{
    public string? Notes { get; init; }
    public List<VehicleDto> Vehicles { get; init; } = [];
}

public record VehicleDto
{
    public int Id { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsDelivered { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public string? DeliveredBy { get; init; }
}

public record CreateContainerRequest
{
    public string ContainerNumber { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public record UpdateContainerRequest
{
    public string? Notes { get; init; }
}

public record DashboardStatsDto
{
    public int TotalContainers { get; init; }
    public int TotalVehicles { get; init; }
    public int PendingContainers { get; init; }
    public int CompletedContainers { get; init; }
    public int InProgressContainers { get; init; }
    public int DeliveredVehicles { get; init; }
    public int UndeliveredVehicles { get; init; }
}