using System.ComponentModel.DataAnnotations;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Application.DTOs;

public class VehicleSearchDto
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ContainerId { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class DeliverVehicleRequest
{
    [Required(ErrorMessage = "VIN is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "VIN is required")]
    public string Vin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Container ID is required")]
    public int ContainerId { get; set; }

    [Required(ErrorMessage = "Delivery method is required")]
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Manual;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    [StringLength(50, ErrorMessage = "Scanned VIN cannot exceed 50 characters")]
    public string? ScannedVin { get; set; }
}

public class BatchDeliveryItemRequest
{
    [Required(ErrorMessage = "VIN is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "VIN is required")]
    public string Vin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Delivery method is required")]
    public DeliveryMethod Method { get; set; } = DeliveryMethod.Manual;

    [StringLength(50, ErrorMessage = "Scanned VIN cannot exceed 50 characters")]
    public string? ScannedVin { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
}

public class BatchDeliveryRequest
{
    [Required(ErrorMessage = "Container ID is required")]
    public int ContainerId { get; set; }

    [Required(ErrorMessage = "At least one delivery item is required")]
    [MinLength(1, ErrorMessage = "At least one delivery item is required")]
    public List<BatchDeliveryItemRequest> Items { get; set; } = [];
}

public class UndeliverRequest
{
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}

public class DeliveryResponse
{
    public bool Success { get; set; }
    public VehicleDto? Vehicle { get; set; }
    public ContainerDto? Container { get; set; }
    public string? Warning { get; set; }
    public string? Error { get; set; }
}

public class BatchDeliveryResponse
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchDeliveryErrorDto> Errors { get; set; } = [];
}

public class BatchDeliveryErrorDto
{
    public string Vin { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}