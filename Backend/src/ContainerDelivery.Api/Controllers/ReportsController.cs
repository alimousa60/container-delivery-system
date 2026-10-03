using ContainerDelivery.Api.Models;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerDelivery.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Generate PDF report for a container
    /// </summary>
    [HttpPost("container/{containerId}")]
    [ProducesResponseType(typeof(ContainerReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReport(int containerId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var report = await _reportService.GenerateAsync(containerId, userId);
        
        return Ok(MapToDto(report));
    }

    /// <summary>
    /// Get all reports for a container
    /// </summary>
    [HttpGet("container/{containerId}")]
    [ProducesResponseType(typeof(List<ContainerReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(int containerId)
    {
        var reports = await _reportService.GetByContainerIdAsync(containerId);
        
        return Ok(reports.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Download a report file
    /// </summary>
    [HttpGet("download/{reportId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReport(int reportId)
    {
        var stream = await _reportService.DownloadAsync(reportId);
        var report = await _reportService.GetByIdAsync(reportId);
        
        return File(stream, "application/pdf", report?.FileName ?? $"report_{reportId}.pdf");
    }

    /// <summary>
    /// Generate reports for multiple containers (Admin only)
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(BulkReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateBulkReports([FromBody] BulkReportRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var result = await _reportService.GenerateBulkAsync(request.ContainerIds, userId);
        
        return Ok(new BulkReportResponse
        {
            SuccessCount = result.SuccessCount,
            FailureCount = result.FailureCount,
            Errors = result.Errors.Select(e => new BulkReportErrorDto { ContainerId = e.ContainerId, Error = e.Error }).ToList()
        });
    }

    private static ContainerReportDto MapToDto(ContainerReport report)
    {
        return new ContainerReportDto
        {
            Id = report.Id,
            ContainerId = report.ContainerId,
            ContainerNumber = report.Container?.ContainerNumber ?? string.Empty,
            FileName = report.FileName,
            GeneratedAt = report.GeneratedAt,
            GeneratedBy = report.GeneratedByUser?.FullName ?? "Unknown",
            TotalVehicles = report.TotalVehicles,
            DeliveredVehicles = report.DeliveredVehicles,
            UndeliveredVehicles = report.UndeliveredVehicles,
            CompletionPercentage = report.CompletionPercentage
        };
    }
}