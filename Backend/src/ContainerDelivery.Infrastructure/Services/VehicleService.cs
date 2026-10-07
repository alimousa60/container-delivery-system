using Microsoft.Extensions.Logging;
using ContainerDelivery.Application.DTOs;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Exceptions;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;
using InvalidOperationException = ContainerDelivery.Core.Exceptions.InvalidOperationException;

namespace ContainerDelivery.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IContainerService _containerService;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IContainerService containerService,
        ILogger<VehicleService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _containerService = containerService;
        _logger = logger;
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        var spec = new VehicleByIdSpec(id);
        return await _unitOfWork.Vehicles.FirstOrDefaultAsync(spec);
    }

    public async Task<Vehicle?> GetByVinAsync(string vin)
    {
        var spec = new VehicleByVinSpec(vin);
        return await _unitOfWork.Vehicles.FirstOrDefaultAsync(spec);
    }

    public async Task<PagedResult<Vehicle>> SearchAsync(string query, int page, int pageSize)
    {
        var spec = new VehiclesSearchPagedSpec(query, page, pageSize);
        var countSpec = new VehiclesSearchCountSpec(query);

        var items = await _unitOfWork.Vehicles.ListAsync(spec);
        var totalCount = await _unitOfWork.Vehicles.CountAsync(countSpec);

        return new PagedResult<Vehicle>(items, totalCount, page, pageSize);
    }

    public async Task<DeliveryResult> DeliverAsync(string vin, int containerId, int userId, DeliveryMethod method, string? notes = null, string? scannedVin = null)
    {
        var vehicle = await GetByVinAsync(vin);
        if (vehicle == null)
            return new DeliveryResult(false, Error: "Vehicle not found");

        if (vehicle.ContainerId != containerId)
            return new DeliveryResult(false, Error: "Vehicle does not belong to this container");

        if (vehicle.IsDelivered)
            return new DeliveryResult(false, Error: "Vehicle is already delivered");

        if (method == DeliveryMethod.BarcodeScan && !vehicle.ValidateVin(scannedVin!))
            return new DeliveryResult(false, Error: "Scanned VIN does not match vehicle VIN");

        vehicle.MarkAsDelivered(userId, method, notes, scannedVin);
        await _unitOfWork.Vehicles.UpdateAsync(vehicle);

        // Recalculate container totals
        var container = await _containerService.RecalculateTotalsAsync(containerId);

        var warning = container.HasPendingDeliveries 
            ? "Warning: There are still vehicles pending delivery in this container."
            : null;

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Delivery,
            EntityType = EntityType.Vehicle,
            EntityId = vehicle.Id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { IsDelivered = false }),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { vehicle.IsDelivered, vehicle.DeliveredAt, vehicle.DeliveredByUserId }),
            IpAddress = null,
            UserAgent = null
        });

        return new DeliveryResult(true, vehicle, container, warning);
    }

    public async Task<BatchDeliveryResult> DeliverBatchAsync(int containerId, IEnumerable<BatchDeliveryItem> items, int userId)
    {
        var errors = new List<BatchDeliveryError>();
        int successCount = 0;

        foreach (var item in items)
        {
            var result = await DeliverAsync(item.Vin, containerId, userId, item.Method, item.Notes, item.ScannedVin);
            if (result.Success)
            {
                successCount++;
            }
            else
            {
                errors.Add(new BatchDeliveryError(item.Vin, result.Error!));
            }
        }

        return new BatchDeliveryResult(successCount, errors.Count, errors);
    }

    public async Task<Vehicle> UndeliverAsync(int vehicleId, int userId, string? reason = null)
    {
        var vehicle = await GetByIdAsync(vehicleId);
        if (vehicle == null)
            throw new EntityNotFoundException("Vehicle", vehicleId);

        if (!vehicle.IsDelivered)
            throw new InvalidOperationException("Vehicle is not delivered", "VEHICLE_NOT_DELIVERED");

        vehicle.MarkAsUndelivered(userId, reason);
        await _unitOfWork.Vehicles.UpdateAsync(vehicle);

        await _containerService.RecalculateTotalsAsync(vehicle.ContainerId);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Update,
            EntityType = EntityType.Vehicle,
            EntityId = vehicle.Id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { IsDelivered = true }),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { vehicle.IsDelivered }),
            IpAddress = null,
            UserAgent = null
        });

        return vehicle;
    }
}
