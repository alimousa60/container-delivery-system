using ContainerDelivery.Api.Models;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerDelivery.Api.Controllers;

[ApiController]
[Route("api/v1/settings")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Get company settings
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _settingsService.GetAsync();
        
        if (settings == null)
        {
            return NotFound(new { error = "Settings not found" });
        }

        return Ok(MapToDto(settings));
    }

    /// <summary>
    /// Update company settings (Admin only)
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var settings = new ContainerDelivery.Core.Entities.CompanySettings
        {
            CompanyName = request.CompanyName,
            LogoUrl = request.LogoUrl,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            ReportFooter = request.ReportFooter,
            Website = request.Website,
            IsActive = request.IsActive
        };

        var updatedSettings = await _settingsService.UpdateAsync(settings, userId);
        
        return Ok(MapToDto(updatedSettings));
    }

    /// <summary>
    /// Upload company logo
    /// </summary>
    [HttpPost("logo")]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [RequestSizeLimit(5_000_000)] // 5MB max
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/svg+xml", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Invalid file type. Allowed: JPEG, PNG, SVG, WebP" });

        // In a real implementation, upload to blob storage and get URL
        // For now, we'll simulate with a placeholder URL
        var logoUrl = $"/uploads/logo/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var settings = await _settingsService.GetAsync();
        if (settings == null)
        {
            settings = new ContainerDelivery.Core.Entities.CompanySettings();
        }

        settings.LogoUrl = logoUrl;
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var updatedSettings = await _settingsService.UpdateAsync(settings, userId);

        return Ok(MapToDto(updatedSettings));
    }

    /// <summary>
    /// Get company name (public endpoint)
    /// </summary>
    [HttpGet("company-name")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanyName()
    {
        var companyName = await _settingsService.GetCompanyNameAsync();
        return Ok(new { companyName });
    }

    /// <summary>
    /// Get company logo URL (public endpoint)
    /// </summary>
    [HttpGet("logo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogoUrl()
    {
        var logoUrl = await _settingsService.GetLogoUrlAsync();
        return Ok(new { logoUrl });
    }

    /// <summary>
    /// Get report footer (public endpoint)
    /// </summary>
    [HttpGet("report-footer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportFooter()
    {
        var footer = await _settingsService.GetReportFooterAsync();
        return Ok(new { footer });
    }

    private static CompanySettingsDto MapToDto(ContainerDelivery.Core.Entities.CompanySettings settings)
    {
        return new CompanySettingsDto
        {
            Id = settings.Id,
            CompanyName = settings.CompanyName,
            LogoUrl = settings.LogoUrl,
            Address = settings.Address,
            Phone = settings.Phone,
            Email = settings.Email,
            ReportFooter = settings.ReportFooter,
            Website = settings.Website,
            IsActive = settings.IsActive,
            CreatedAt = settings.CreatedAt,
            LastModifiedAt = settings.LastModifiedAt,
            LastModifiedBy = settings.LastModifiedByUser?.FullName
        };
    }
}