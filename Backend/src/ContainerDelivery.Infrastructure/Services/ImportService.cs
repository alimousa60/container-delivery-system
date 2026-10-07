using ClosedXML.Excel;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Exceptions;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;
using Microsoft.Extensions.Logging;

namespace ContainerDelivery.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<ImportService> _logger;

    public ImportService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<ImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ImportResultDto> ImportAsync(Stream excelStream, string fileName, int importedByUserId, string? containerNumberPrefix = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var importBatch = new ImportBatch(fileName, $"uploads/{Guid.NewGuid()}_{fileName}", importedByUserId);
        await _unitOfWork.ImportBatches.AddAsync(importBatch);
        await _unitOfWork.SaveChangesAsync();

        importBatch.StartProcessing();
        await _unitOfWork.ImportBatches.UpdateAsync(importBatch);

        var result = new ImportResultDto
        {
            ImportBatchId = importBatch.Id
        };

        var errors = new List<ImportErrorDto>();
        var seenVins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var containerCache = new Dictionary<string, Container>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);
            
            // Validate headers
            var headerRow = worksheet.Row(1);
            var headerValidation = ValidateHeaders(headerRow);
            if (!headerValidation.IsValid)
            {
                importBatch.Fail($"Invalid headers: {string.Join(", ", headerValidation.Errors)}");
                await _unitOfWork.ImportBatches.UpdateAsync(importBatch);
                throw new ImportException("Invalid Excel headers", new { headerValidation.Errors });
            }

            var rows = worksheet.RowsUsed().Skip(1).ToList(); // Skip header
            result.TotalRows = rows.Count;

            foreach (var row in rows)
            {
                var rowNumber = row.RowNumber();
                try
                {
                    var validationResult = ValidateRow(row, rowNumber);
                    if (!validationResult.IsValid)
                    {
                        errors.AddRange(validationResult.Errors);
                        result.InvalidRows++;
                        continue;
                    }

                    var containerNumber = row.Cell(1).GetString().Trim();
                    var vin = row.Cell(2).GetString().Trim().ToUpperInvariant();
                    var description = row.Cell(3).GetString().Trim();

                    // Check for duplicate VIN in this import
                    if (!seenVins.Add(vin))
                    {
                        errors.Add(new ImportErrorDto
                        {
                            Row = rowNumber,
                            Field = "VIN",
                            Value = vin,
                            Error = $"Duplicate VIN in import file: {vin}"
                        });
                        result.DuplicateVins++;
                        result.InvalidRows++;
                        continue;
                    }

                    // Check for existing VIN in database
                    var existingVehicle = await _unitOfWork.Vehicles.FirstOrDefaultAsync(
                        new VehicleByVinSpec(vin));
                    
                    if (existingVehicle != null)
                    {
                        errors.Add(new ImportErrorDto
                        {
                            Row = rowNumber,
                            Field = "VIN",
                            Value = vin,
                            Error = $"VIN already exists in system: {vin}"
                        });
                        result.DuplicateVins++;
                        result.InvalidRows++;
                        continue;
                    }

                    var fullContainerNumber = string.IsNullOrEmpty(containerNumberPrefix) 
                        ? containerNumber 
                        : $"{containerNumberPrefix}_{containerNumber}";

                    // Get or create container
                    Container container;
                    if (containerCache.TryGetValue(fullContainerNumber, out var cachedContainer))
                    {
                        container = cachedContainer;
                    }
                    else
                    {
                        var existingContainer = await _unitOfWork.Containers.FirstOrDefaultAsync(
                            new ContainerByNumberSpec(fullContainerNumber));
                        
                        if (existingContainer != null)
                        {
                            container = existingContainer;
                        }
                        else
                        {
                            container = new Container(fullContainerNumber, importedByUserId);
                            await _unitOfWork.Containers.AddAsync(container);
                            result.ContainersCreated++;
                        }
                        containerCache[fullContainerNumber] = container;
                    }

                    // Create vehicle
                    var vehicle = new Vehicle(vin, description, container.Id);
                    container.AddVehicle(vehicle);
                    await _unitOfWork.Vehicles.AddAsync(vehicle);
                    await _unitOfWork.Containers.UpdateAsync(container);

                    result.ValidRows++;
                    result.VehiclesImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {RowNumber} in import {ImportBatchId}", rowNumber, importBatch.Id);
                    errors.Add(new ImportErrorDto
                    {
                        Row = rowNumber,
                        Error = ex.Message
                    });
                    result.InvalidRows++;
                }
            }

            result.SuccessfulRecords = result.VehiclesImported;
            result.FailedRecords = result.InvalidRows + result.DuplicateVins;

            var errorJson = System.Text.Json.JsonSerializer.Serialize(errors);
            importBatch.Complete(result.SuccessfulRecords, result.FailedRecords, errorJson);
            await _unitOfWork.ImportBatches.UpdateAsync(importBatch);

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
            result.Errors = errors;

            await _auditService.LogAsync(new AuditLog
            {
                UserId = importedByUserId,
                Action = AuditAction.Import,
                EntityType = EntityType.ImportBatch,
                EntityId = importBatch.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    importBatch.TotalRecords,
                    importBatch.SuccessfulRecords,
                    importBatch.FailedRecords,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                }),
                IpAddress = "system",
                UserAgent = "ImportService"
            });

            return result;
        }
        catch (Exception ex) when (ex is not ImportException)
        {
            _logger.LogError(ex, "Error processing import batch {ImportBatchId}", importBatch.Id);
            importBatch.Fail(ex.Message);
            await _unitOfWork.ImportBatches.UpdateAsync(importBatch);
            throw new ImportException("Failed to process import", new { importBatch.Id, ex.Message });
        }
    }

    public async Task<ImportBatch> GetByIdAsync(int id)
    {
        return await _unitOfWork.ImportBatches.GetByIdAsync(id);
    }

    public async Task<ImportBatchStatusDto> GetStatusAsync(int importBatchId)
    {
        var batch = await _unitOfWork.ImportBatches.GetByIdAsync(importBatchId);
        if (batch == null)
            throw new EntityNotFoundException("ImportBatch", importBatchId);

        var errors = new List<ImportErrorDto>();
        if (!string.IsNullOrEmpty(batch.ErrorDetails))
        {
            try
            {
                errors = System.Text.Json.JsonSerializer.Deserialize<List<ImportErrorDto>>(batch.ErrorDetails) ?? [];
            }
            catch { }
        }

        return new ImportBatchStatusDto
        {
            Id = batch.Id,
            FileName = batch.FileName,
            Status = batch.Status,
            TotalRecords = batch.TotalRecords,
            SuccessfulRecords = batch.SuccessfulRecords,
            FailedRecords = batch.FailedRecords,
            ImportedAt = batch.ImportedAt,
            CompletedAt = batch.CompletedAt,
            Errors = errors
        };
    }

    public async Task<PagedResult<ImportBatch>> GetPagedAsync(int page, int pageSize)
    {
        var spec = new ImportBatchesPagedSpec(page, pageSize);
        var countSpec = new ImportBatchesCountSpec();

        var items = await _unitOfWork.ImportBatches.ListAsync(spec);
        var totalCount = await _unitOfWork.ImportBatches.CountAsync(countSpec);

        return new PagedResult<ImportBatch>(items, totalCount, page, pageSize);
    }

    private (bool IsValid, List<string> Errors) ValidateHeaders(IXLRow headerRow)
    {
        var errors = new List<string>();
        var expectedHeaders = new[] { "Container Number", "VIN", "Vehicle Description" };
        
        for (int i = 0; i < expectedHeaders.Length; i++)
        {
            var cellValue = headerRow.Cell(i + 1).GetString().Trim();
            if (!string.Equals(cellValue, expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Column {i + 1}: Expected '{expectedHeaders[i]}', found '{cellValue}'");
            }
        }

        return (errors.Count == 0, errors);
    }

    private (bool IsValid, List<ImportErrorDto> Errors) ValidateRow(IXLRow row, int rowNumber)
    {
        var errors = new List<ImportErrorDto>();

        // Container Number
        var containerNumber = row.Cell(1).GetString().Trim();
        if (string.IsNullOrWhiteSpace(containerNumber))
        {
            errors.Add(new ImportErrorDto
            {
                Row = rowNumber,
                Field = "Container Number",
                Value = containerNumber,
                Error = "Container number is required"
            });
        }
        else if (containerNumber.Length > 50)
        {
            errors.Add(new ImportErrorDto
            {
                Row = rowNumber,
                Field = "Container Number",
                Value = containerNumber,
                Error = "Container number cannot exceed 50 characters"
            });
        }

        // VIN
        var vin = row.Cell(2).GetString().Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(vin))
        {
            errors.Add(new ImportErrorDto
            {
                Row = rowNumber,
                Field = "VIN",
                Value = vin,
                Error = "VIN is required"
            });
        }
        else if (vin.Length != 17)
        {
            errors.Add(new ImportErrorDto
            {
                Row = rowNumber,
                Field = "VIN",
                Value = vin,
                Error = "VIN must be exactly 17 characters"
            });
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(vin, @"^[A-HJ-NPR-Z0-9]{17}$"))
        {
            errors.Add(new ImportErrorDto
            {
                Row = rowNumber,
                Field = "VIN",
                Value = vin,
                Error = "Invalid VIN format (valid characters: A-H, J-N, P-R, Z, 0-9)"
            });
        }

        // Description
        var description = row.Cell(3).GetString().Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add(new ImportErrorDto
            {
                Row = rowNumber,
                Field = "Vehicle Description",
                Value = description,
                Error = "Vehicle description is required"
            });
        }
        else if (description.Length > 500)
        {
            errors.Add(new ImportErrorDto
            {
                Row = rowNumber,
                Field = "Vehicle Description",
                Value = description,
                Error = "Vehicle description cannot exceed 500 characters"
            });
        }

        return (errors.Count == 0, errors);
    }
}