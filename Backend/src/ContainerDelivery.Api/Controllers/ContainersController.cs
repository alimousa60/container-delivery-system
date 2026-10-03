using ContainerDelivery.Api.Models;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerDelivery.Api.Controllers;

[ApiController]
[Route("api/v1/containers")]
[Authorize]
[Produces("application/json")]
public class ContainersController : ControllerBase
{
    private readonly IContainerService _containerService;
    private readonly ILogger<ContainersController> _logger;

    public ContainersController(IContainerService containerService, ILogger<ContainersController> logger)
    {
        _containerService = containerService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ContainerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContainers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ContainerStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        var result = await _containerService.GetPagedAsync(page, pageSize, status, search, sortBy, sortOrder);
        
        var items = result.Items.Select(MapToDto).ToList();
        
        return Ok(PagedResponse<ContainerDto>.Create(items, result.TotalCount, page, pageSize));
    }

    [HttpGet("dashboard/stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = await _containerService.GetDashboardStatsAsync();
        
        return Ok(new DashboardStatsDto
        {
            TotalContainers = stats.TotalContainers,
            TotalVehicles = stats.TotalVehicles,
            PendingContainers = stats.PendingContainers,
            CompletedContainers = stats.CompletedContainers,
            InProgressContainers = stats.InProgressContainers,
            DeliveredVehicles = stats.DeliveredVehicles,
            UndeliveredVehicles = stats.UndeliveredVehicles
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ContainerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContainer(int id)
    {
        var container = await _containerService.GetByIdAsync(id);
        
        if (container == null)
            return NotFound();

        var vehicles = await _containerService.GetVehiclesAsync(id);
        var vehicleDtos = vehicles.Select(MapToVehicleDto).ToList();

        return Ok(new ContainerDetailDto
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
            CompletedAt = container.CompletedAt,
            Notes = container.Notes,
            Vehicles = vehicleDtos
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContainer([FromBody] CreateContainerRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var container = await _containerService.CreateAsync(request.ContainerNumber, userId, request.Notes);
        
        return CreatedAtAction(nameof(GetContainer), new { id = container.Id }, MapToDto(container));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContainer(int id, [FromBody] UpdateContainerRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var container = await _containerService.UpdateAsync(id, request.Notes, userId);
        
        return Ok(MapToDto(container));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteContainer(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        await _containerService.DeleteAsync(id, userId);
        
        return NoContent();
    }

    [HttpPost("{id}/start-delivery")]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartDelivery(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var container = await _containerService.StartDeliveryAsync(id, userId);
        
        return Ok(MapToDto(container));
    }

    [HttpPost("{id}/close")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseContainer(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var container = await _containerService.CloseContainerAsync(id, userId);
        
        return Ok(MapToDto(container));
    }

    [HttpPost("{id}/reopen")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReopenContainer(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var container = await _containerService.ReopenContainerAsync(id, userId);
        
        return Ok(MapToDto(container));
    }

    [HttpGet("archive")]
    [ProducesResponseType(typeof(PagedResponse<ContainerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArchivedContainers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await _containerService.GetArchiveAsync(page, pageSize, search);
        
        var items = result.Items.Select(MapToDto).ToList();
        
        return Ok(PagedResponse<ContainerDto>.Create(items, result.TotalCount, page, pageSize));
    }

    [HttpGet("{id}/vehicles")]
    [ProducesResponseType(typeof(PagedResponse<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVehicles(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var vehicles = await _containerService.GetVehiclesAsync(id);
        
        var items = vehicles.Select(MapToVehicleDto).ToList();
        
        return Ok(PagedResponse<VehicleDto>.Create(items, items.Count, page, pageSize));
    }

    [HttpPost("import")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> ImportVehicles(
        IFormFile file,
        [FromForm] string? containerNumberPrefix = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        using var stream = file.OpenReadStream();
        var result = await _containerService.ImportAsync(stream, file.FileName, userId, containerNumberPrefix);
        
        return Ok(result);
    }

    [HttpGet("import/{importBatchId}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ImportBatchStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImportStatus(int importBatchId)
    {
        var status = await _containerService.GetImportStatusAsync(importBatchId);
        return Ok(status);
    }

    private static ContainerDto MapToDto(Container container)
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
            CompletedAt = container.CompletedAt,
            ClosedAt = container.ClosedAt,
            ClosedBy = container.ClosedByUser?.FullName
        };
    }

    private static VehicleDto MapToVehicleDto(Vehicle vehicle)
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
}