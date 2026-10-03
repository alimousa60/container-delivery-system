using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Api.Models;

public record VehicleSearchDto
{
    public int Id { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int ContainerId { get; init; }
    public string ContainerNumber { get; init; } = string.Empty;
    public bool IsDelivered { get; init; }
    public DateTime? DeliveredAt { get; init; }
}

public record DeliverVehicleRequest
{
    public string Vin { get; init; } = string.Empty;
    public int ContainerId { get; init; }
    public DeliveryMethod DeliveryMethod { get; init; } = DeliveryMethod.Manual;
    public string? Notes { get; init; }
    public string? ScannedVin { get; init; }
}

public record BatchDeliveryItemRequest
{
    public string Vin { get; init; } = string.Empty;
    public DeliveryMethod Method { get; init; } = DeliveryMethod.Manual;
    public string? ScannedVin { get; init; }
    public string? Notes { get; init; }
}

public record BatchDeliveryRequest
{
    public int ContainerId { get; init; }
    public List<BatchDeliveryItemRequest> Items { get; init; } = [];
}

public record UndeliverRequest
{
    public string? Reason { get; init; }
}

public record DeliveryResponse
{
    public bool Success { get; init; }
    public VehicleDto? Vehicle { get; init; }
    public ContainerDto? Container { get; init; }
    public string? Warning { get; init; }
    public string? Error { get; init; }
}

public record BatchDeliveryResponse
{
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<BatchDeliveryErrorDto> Errors { get; init; } = [];
}

public record BatchDeliveryErrorDto
{
    public string Vin { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}