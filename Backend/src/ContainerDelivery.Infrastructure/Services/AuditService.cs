using ClosedXML.Excel;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;

namespace ContainerDelivery.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogAsync(AuditLog auditLog)
    {
        await _unitOfWork.AuditLogs.AddAsync(auditLog);
    }

    public async Task<PagedResult<AuditLog>> GetPagedAsync(int page, int pageSize, int? userId = null, AuditAction? action = null, EntityType? entityType = null, int? entityId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var spec = new AuditLogsPagedSpec(page, pageSize, userId, action, entityType, entityId, fromDate, toDate);
        var countSpec = new AuditLogsCountSpec(userId, action, entityType, entityId, fromDate, toDate);

        var items = await _unitOfWork.AuditLogs.ListAsync(spec);
        var totalCount = await _unitOfWork.AuditLogs.CountAsync(countSpec);

        return new PagedResult<AuditLog>(items, totalCount, page, pageSize);
    }

    public async Task<Stream> ExportAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var spec = new AuditLogsExportSpec(fromDate, toDate);
        var logs = await _unitOfWork.AuditLogs.ListAsync(spec);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Audit Logs");

        // Headers
        worksheet.Cell(1, 1).Value = "Timestamp";
        worksheet.Cell(1, 2).Value = "User";
        worksheet.Cell(1, 3).Value = "Action";
        worksheet.Cell(1, 4).Value = "Entity Type";
        worksheet.Cell(1, 5).Value = "Entity ID";
        worksheet.Cell(1, 6).Value = "Old Values";
        worksheet.Cell(1, 7).Value = "New Values";
        worksheet.Cell(1, 8).Value = "IP Address";
        worksheet.Cell(1, 9).Value = "User Agent";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data
        for (int i = 0; i < logs.Count; i++)
        {
            var log = logs[i];
            var row = i + 2;
            worksheet.Cell(row, 1).Value = log.Timestamp;
            worksheet.Cell(row, 2).Value = log.User?.Email ?? "System";
            worksheet.Cell(row, 3).Value = log.Action.ToString();
            worksheet.Cell(row, 4).Value = log.EntityType.ToString();
            worksheet.Cell(row, 5).Value = log.EntityId;
            worksheet.Cell(row, 6).Value = log.OldValues ?? "";
            worksheet.Cell(row, 7).Value = log.NewValues ?? "";
            worksheet.Cell(row, 8).Value = log.IpAddress ?? "";
            worksheet.Cell(row, 9).Value = log.UserAgent ?? "";
        }

        worksheet.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}