using ContainerDelivery.Api.Models;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerDelivery.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize]
[Produces("application/json")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(IVehicleService vehicleService, ILogger<VehiclesController> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    /// <summary>
    /// Search vehicles by VIN, container number, or description
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResponse<VehicleSearchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search query is required" });

        var result = await _vehicleService.SearchAsync(q, page, pageSize);
        
        var items = result.Items.Select(MapToSearchDto).ToList();
        
        return Ok(PagedResponse<VehicleSearchDto>.Create(items, result.TotalCount, page, pageSize));
    }

    /// <summary>
    /// Deliver a single vehicle
    /// </summary>
    [HttpPost("deliver")]
    [ProducesResponseType(typeof(DeliveryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deliver([FromBody] DeliverVehicleRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var result = await _vehicleService.DeliverAsync(
            request.Vin, 
            request.ContainerId, 
            userId, 
            request.DeliveryMethod, 
            request.Notes, 
            request.ScannedVin);

        if (!result.Success)
            return BadRequest(new { error = result.Error, warning = result.Warning });

        return Ok(new DeliveryResponse
        {
            Success = true,
            Vehicle = result.Vehicle != null ? MapToDto(result.Vehicle) : null,
            Container = result.Container != null ? MapToContainerDto(result.Container) : null,
            Warning = result.Warning
        });
    }

    /// <summary>
    /// Deliver multiple vehicles in batch
    /// </summary>
    [HttpPost("deliver/batch")]
    [ProducesResponseType(typeof(BatchDeliveryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeliverBatch([FromBody] BatchDeliveryRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var items = request.Items.Select(i => new ContainerDelivery.Core.Interfaces.BatchDeliveryItem(
            i.Vin, i.Method, i.ScannedVin, i.Notes)).ToList();
        
        var result = await _vehicleService.DeliverBatchAsync(request.ContainerId, items, userId);

        return Ok(new BatchDeliveryResponse
        {
            SuccessCount = result.SuccessCount,
            FailureCount = result.FailureCount,
            Errors = result.Errors.Select(e => new BatchDeliveryErrorDto { Vin = e.Vin, Error = e.Error }).ToList()
        });
    }

    /// <summary>
    /// Mark vehicle as undelivered (Admin only)
    /// </summary>
    [HttpPut("{id}/undeliver")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Undeliver(int id, [FromBody] UndeliverRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var vehicle = await _vehicleService.UndeliverAsync(id, userId, request.Reason);
        
        return Ok(MapToDto(vehicle));
    }

    private static VehicleSearchDto MapToSearchDto(Vehicle vehicle)
    {
        return new VehicleSearchDto
        {
            Id = vehicle.Id,
            Vin = vehicle.Vin,
            Description = vehicle.Description,
            ContainerId = vehicle.ContainerId,
            ContainerNumber = vehicle.Container?.ContainerNumber ?? string.Empty,
            IsDelivered = vehicle.IsDelivered,
            DeliveredAt = vehicle.DeliveredAt
        };
    }

    private static VehicleDto MapToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            Vin = vehicle.Vin,
            Description = vehicle.Description,
            IsDelivered = vehicle.IsDelivered,
            DeliveredAt = vehicle.DeliveredAt,
            DeliveredBy = vehicle.DeliveredByUser?.FullName
        };
    }

    private static ContainerDto MapToContainerDto(Container container)
    {
        return new ContainerDto
        {
            Id = container.Id,
            ContainerNumber = container.ContainerNumber,
            TotalVehicles = container.TotalVehicles,
            DeliveredVehicles = container.DeliveredVehicles,
            Status = container.Status,
            CompletionPercentage = container.CompletionPercentage,
            CreatedBy = container.CreatedByUser?.FullName ?? "Unknown",
            CreatedAt = container.CreatedAt,
            StartedAt = container.StartedAt,
            CompletedAt = container.CompletedAt
        };
    }
}