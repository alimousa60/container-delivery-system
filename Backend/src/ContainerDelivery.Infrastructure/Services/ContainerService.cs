using ContainerDelivery.Application.DTOs;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Exceptions;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ContainerDelivery.Infrastructure.Services;

public class ContainerService : IContainerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<ContainerService> _logger;

    public ContainerService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<ContainerService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Container?> GetByIdAsync(int id)
    {
        var spec = new ContainerByIdSpec(id);
        return await _unitOfWork.Containers.FirstOrDefaultAsync(spec);
    }

    public async Task<Container?> GetByNumberAsync(string containerNumber)
    {
        var spec = new ContainerByNumberSpec(containerNumber);
        return await _unitOfWork.Containers.FirstOrDefaultAsync(spec);
    }

    public async Task<PagedResult<Container>> GetPagedAsync(int page, int pageSize, ContainerStatus? status = null, string? search = null, string? sortBy = null, string? sortOrder = null)
    {
        var spec = new ContainersPagedSpec(page, pageSize, status, search, sortBy, sortOrder);
        var countSpec = new ContainersCountSpec(status, search);

        var items = await _unitOfWork.Containers.ListAsync(spec);
        var totalCount = await _unitOfWork.Containers.CountAsync(countSpec);

        return new PagedResult<Container>(items, totalCount, page, pageSize);
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var totalContainers = await _unitOfWork.Containers.CountAsync(new ContainersCountSpec());
        var totalVehicles = await _unitOfWork.Vehicles.CountAsync(new VehiclesCountSpec());
        var pendingContainers = await _unitOfWork.Containers.CountAsync(new ContainersCountSpec(ContainerStatus.NotStarted));
        var completedContainers = await _unitOfWork.Containers.CountAsync(new ContainersCountSpec(ContainerStatus.FullyDelivered));
        var inProgressContainers = await _unitOfWork.Containers.CountAsync(new ContainersCountSpec(ContainerStatus.InProgress));
        var closedContainers = await _unitOfWork.Containers.CountAsync(new ContainersCountSpec(ContainerStatus.Closed));
        var deliveredVehicles = await _unitOfWork.Vehicles.CountAsync(new VehiclesByDeliveredStatusSpec(true));
        var undeliveredVehicles = await _unitOfWork.Vehicles.CountAsync(new VehiclesByDeliveredStatusSpec(false));

        return new DashboardStats(
            totalContainers,
            totalVehicles,
            pendingContainers,
            completedContainers,
            inProgressContainers,
            deliveredVehicles,
            undeliveredVehicles
        );
    }

    public async Task<Container> CreateAsync(string containerNumber, int createdByUserId, string? notes = null)
    {
        var existing = await GetByNumberAsync(containerNumber);
        if (existing != null)
            throw new DuplicateEntityException("Container", "ContainerNumber", containerNumber);

        var container = new Container(containerNumber, createdByUserId, notes);
        await _unitOfWork.Containers.AddAsync(container);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = AuditAction.Create,
            EntityType = EntityType.Container,
            EntityId = container.Id,
            OldValues = null,
            NewValues = JsonSerializer.Serialize(new { container.ContainerNumber, container.TotalVehicles, container.Status }),
            IpAddress = null,
            UserAgent = null
        });

        return container;
    }

    public async Task<Container> UpdateAsync(int id, string? notes, int updatedByUserId)
    {
        var container = await GetByIdAsync(id);
        if (container == null)
            throw new EntityNotFoundException("Container", id);

        var oldValues = new { container.Notes };
        container.UpdateNotes(notes);
        await _unitOfWork.Containers.UpdateAsync(container);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = updatedByUserId,
            Action = AuditAction.Update,
            EntityType = EntityType.Container,
            EntityId = container.Id,
            OldValues = JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(new { container.Notes }),
            IpAddress = null,
            UserAgent = null
        });

        return container;
    }

    public async Task<bool> DeleteAsync(int id, int deletedByUserId)
    {
        var container = await GetByIdAsync(id);
        if (container == null)
            throw new EntityNotFoundException("Container", id);

        if (!container.CanBeDeleted)
            throw new InvalidOperationException("Only fully delivered or closed containers can be deleted", "CANNOT_DELETE_CONTAINER", new { container.Status });

        await _unitOfWork.Containers.DeleteAsync(container);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = deletedByUserId,
            Action = AuditAction.Delete,
            EntityType = EntityType.Container,
            EntityId = container.Id,
            OldValues = JsonSerializer.Serialize(new { container.ContainerNumber, container.TotalVehicles, container.DeliveredVehicles, container.Status }),
            NewValues = null,
            IpAddress = null,
            UserAgent = null
        });

        return true;
    }

    public async Task<Container> StartDeliveryAsync(int containerId, int userId)
    {
        var container = await GetByIdAsync(containerId);
        if (container == null)
            throw new EntityNotFoundException("Container", containerId);

        if (container.Status != ContainerStatus.NotStarted)
            throw new InvalidOperationException("Container delivery has already started", "INVALID_CONTAINER_STATUS", new { container.Status });

        var oldStatus = container.Status;
        container.StartDelivery();
        await _unitOfWork.Containers.UpdateAsync(container);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Update,
            EntityType = EntityType.Container,
            EntityId = container.Id,
            OldValues = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValues = JsonSerializer.Serialize(new { container.Status }),
            IpAddress = null,
            UserAgent = null
        });

        return container;
    }

    public async Task<Container> CloseContainerAsync(int containerId, int userId)
    {
        var container = await GetByIdAsync(containerId);
        if (container == null)
            throw new EntityNotFoundException("Container", containerId);

        if (!container.CanBeClosed)
            throw new InvalidOperationException("Container can only be closed when fully delivered and a report has been generated", "CANNOT_CLOSE_CONTAINER", new { container.Status, container.Reports.Count() });

        var oldStatus = container.Status;
        container.CloseContainer(userId);
        await _unitOfWork.Containers.UpdateAsync(container);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Update,
            EntityType = EntityType.Container,
            EntityId = container.Id,
            OldValues = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValues = JsonSerializer.Serialize(new { container.Status, container.ClosedAt, container.ClosedByUserId }),
            IpAddress = null,
            UserAgent = null
        );

        return container;
    }

    public async Task<Container> ReopenContainerAsync(int containerId, int userId)
    {
        var container = await GetByIdAsync(containerId);
        if (container == null)
            throw new EntityNotFoundException("Container", containerId);

        if (!container.CanBeReopened)
            throw new InvalidOperationException("Container can only be reopened if it is currently closed", "CANNOT_REOPEN_CONTAINER", new { container.Status });

        var oldStatus = container.Status;
        container.ReopenContainer(userId);
        await _unitOfWork.Containers.UpdateAsync(container);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Update,
            EntityType = EntityType.Container,
            EntityId = container.Id,
            OldValues = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValues = JsonSerializer.Serialize(new { container.Status, container.ClosedAt, container.ClosedByUserId }),
            IpAddress = null,
            UserAgent = null
        });

        return container;
    }

    public async Task<Container> RecalculateTotalsAsync(int containerId)
    {
        var container = await GetByIdAsync(containerId);
        if (container == null)
            throw new EntityNotFoundException("Container", containerId);

        container.RecalculateTotals();
        await _unitOfWork.Containers.UpdateAsync(container);
        return container;
    }

    public async Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(int containerId)
    {
        var spec = new VehiclesByContainerSpec(containerId);
        return await _unitOfWork.Vehicles.ListAsync(spec);
    }

    public async Task<IReadOnlyList<Vehicle>> GetUndeliveredVehiclesAsync(int containerId)
    {
        var spec = new UndeliveredVehiclesByContainerSpec(containerId);
        return await _unitOfWork.Vehicles.ListAsync(spec);
    }

    public async Task<IReadOnlyList<Vehicle>> GetDeliveredVehiclesAsync(int containerId)
    {
        var spec = new DeliveredVehiclesByContainerSpec(containerId);
        return await _unitOfWork.Vehicles.ListAsync(spec);
    }

    public async Task<ImportResultDto> ImportAsync(Stream excelStream, string fileName, int importedByUserId, string? containerNumberPrefix = null)
    {
        var importBatch = new ImportBatch(fileName, $"uploads/{Guid.NewGuid()}_{fileName}", importedByUserId);
        await _unitOfWork.ImportBatches.AddAsync(importBatch);
        await _unitOfWork.SaveChangesAsync();

        importBatch.StartProcessing();

        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);
            
            var rows = worksheet.RowsUsed().Skip(1).ToList(); // Skip header
            var errors = new List<ImportErrorDto>();
            int successful = 0;
            int failed = 0;

            var containerCache = new Dictionary<string, Container>();

            foreach (var row in rows)
            {
                try
                {
                    var containerNumber = row.Cell(1).GetString().Trim();
                    var vin = row.Cell(2).GetString().Trim().ToUpper();
                    var description = row.Cell(3).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(containerNumber) || string.IsNullOrWhiteSpace(vin) || string.IsNullOrWhiteSpace(description))
                    {
                        errors.Add(new ImportErrorDto(row.RowNumber(), "ContainerNumber/VIN/Description", containerNumber + "|" + vin + "|" + description, "Missing required fields"));
                        failed++;
                        continue;
                    }

                    if (vin.Length != 17)
                    {
                        errors.Add(new ImportErrorDto(row.RowNumber(), "VIN", vin, "Invalid VIN format (must be 17 characters)"));
                        failed++;
                        continue;
                    }

                    var existingVehicle = await _unitOfWork.Vehicles.FirstOrDefaultAsync(new VehicleByVinSpec(vin));
                    if (existingVehicle != null)
                    {
                        errors.Add(new ImportErrorDto(row.RowNumber(), "VIN", vin, $"Duplicate VIN: {vin}"));
                        failed++;
                        continue;
                    }

                    var fullContainerNumber = string.IsNullOrEmpty(containerNumberPrefix) ? containerNumber : $"{containerNumberPrefix}_{containerNumber}";
                    
                    if (!containerCache.TryGetValue(fullContainerNumber, out var container))
                    {
                        var existingContainer = await _unitOfWork.Containers.FirstOrDefaultAsync(new ContainerByNumberSpec(fullContainerNumber));
                        if (existingContainer != null)
                        {
                            container = existingContainer;
                        }
                        else
                        {
                            container = new Container(fullContainerNumber, importedByUserId);
                            await _unitOfWork.Containers.AddAsync(container);
                        }
                        containerCache[fullContainerNumber] = container;
                    }

                    var vehicle = new Vehicle(vin, description, container.Id);
                    container.AddVehicle(vehicle);
                    await _unitOfWork.Vehicles.AddAsync(vehicle);
                    await _unitOfWork.Containers.UpdateAsync(container);

                    successful++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing row {RowNumber}", row.RowNumber());
                    errors.Add(new ImportErrorDto(row.RowNumber(), "All", "", ex.Message));
                    failed++;
                }
            }

            var errorJson = JsonSerializer.Serialize(errors);
            importBatch.Complete(successful, failed, errorJson);
            await _unitOfWork.ImportBatches.UpdateAsync(importBatch);

            await _auditService.LogAsync(new AuditLog
            {
                UserId = importedByUserId,
                Action = AuditAction.Import,
                EntityType = EntityType.ImportBatch,
                EntityId = importBatch.Id,
                OldValues = null,
                NewValues = JsonSerializer.Serialize(new { importBatch.TotalRecords, importBatch.SuccessfulRecords, importBatch.FailedRecords }),
                IpAddress = null,
                UserAgent = null
            });

            return new ImportResultDto
            {
                ImportBatchId = importBatch.Id,
                TotalRows = importBatch.TotalRecords,
                ValidRows = importBatch.SuccessfulRecords,
                InvalidRows = importBatch.FailedRecords,
                DuplicateVins = errors.Count(e => e.Error.Contains("Duplicate")),
                ContainersCreated = containerCache.Count,
                VehiclesImported = successful,
                Errors = errors,
                ProcessingTime = DateTime.UtcNow - importBatch.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing import batch {ImportBatchId}", importBatch.Id);
            importBatch.Fail(ex.Message);
            await _unitOfWork.ImportBatches.UpdateAsync(importBatch);
            throw new ImportException("Failed to process import", new { importBatch.Id, ex.Message });
        }
    }

    public async Task<ImportBatchStatusDto> GetImportStatusAsync(int importBatchId)
    {
        var batch = await _unitOfWork.ImportBatches.GetByIdAsync(importBatchId);
        if (batch == null)
            throw new EntityNotFoundException("ImportBatch", importBatchId);

        var errors = new List<ImportErrorDto>();
        if (!string.IsNullOrEmpty(batch.ErrorDetails))
        {
            try
            {
                errors = JsonSerializer.Deserialize<List<ImportErrorDto>>(batch.ErrorDetails) ?? [];
            }
            catch { }
        }

        return new ImportBatchStatusDto(
            batch.Id,
            batch.FileName,
            batch.Status,
            batch.TotalRecords,
            batch.SuccessfulRecords,
            batch.FailedRecords,
            batch.ImportedAt,
            batch.CompletedAt,
            errors
        );
    }

    public async Task<PagedResult<Container>> GetArchiveAsync(int page, int pageSize, string? search = null)
    {
        var spec = new ContainersArchiveSpec(page, pageSize, search);
        var countSpec = new ContainersArchiveCountSpec(search);

        var items = await _unitOfWork.Containers.ListAsync(spec);
        var totalCount = await _unitOfWork.Containers.CountAsync(countSpec);

        return new PagedResult<Container>(items, totalCount, page, pageSize);
    }
}