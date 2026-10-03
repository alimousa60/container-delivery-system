using ContainerDelivery.Api.Models;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerDelivery.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditService auditService, ILogger<AuditLogsController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated audit logs with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? userId = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] EntityType? entityType = null,
        [FromQuery] int? entityId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _auditService.GetPagedAsync(page, pageSize, userId, action, entityType, entityId, fromDate, toDate);
        
        var items = result.Items.Select(MapToDto).ToList();
        
        return Ok(PagedResponse<AuditLogDto>.Create(items, result.TotalCount, page, pageSize));
    }

    /// <summary>
    /// Export audit logs to Excel
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var stream = await _auditService.ExportAsync(fromDate, toDate);
        
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserEmail = log.User?.Email,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Timestamp = log.Timestamp
        };
    }
}