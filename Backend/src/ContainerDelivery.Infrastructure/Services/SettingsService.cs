using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;
using Microsoft.Extensions.Logging;

namespace ContainerDelivery.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IUnitOfWork unitOfWork, ILogger<SettingsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CompanySettings?> GetAsync()
    {
        var spec = new CompanySettingsSpec();
        return await _unitOfWork.CompanySettings.FirstOrDefaultAsync(spec);
    }

    public async Task<CompanySettings> UpdateAsync(CompanySettings settings, int updatedByUserId)
    {
        var existing = await GetAsync();
        
        if (existing == null)
        {
            settings.Id = 0; // Will be auto-generated
            settings.CreatedAt = DateTime.UtcNow;
            settings.LastModifiedByUserId = updatedByUserId;
            await _unitOfWork.CompanySettings.AddAsync(settings);
        }
        else
        {
            existing.CompanyName = settings.CompanyName;
            existing.LogoUrl = settings.LogoUrl;
            existing.Address = settings.Address;
            existing.Phone = settings.Phone;
            existing.Email = settings.Email;
            existing.ReportFooter = settings.ReportFooter;
            existing.Website = settings.Website;
            existing.IsActive = settings.IsActive;
            existing.LastModifiedByUserId = updatedByUserId;
            existing.LastModifiedAt = DateTime.UtcNow;
            await _unitOfWork.CompanySettings.UpdateAsync(existing);
            settings = existing;
        }

        await _unitOfWork.SaveChangesAsync();
        return settings;
    }

    public async Task<string?> GetLogoUrlAsync()
    {
        var settings = await GetAsync();
        return settings?.LogoUrl;
    }

    public async Task<string> GetCompanyNameAsync()
    {
        var settings = await GetAsync();
        return settings?.CompanyName ?? "Container Delivery System";
    }

    public async Task<string> GetReportFooterAsync()
    {
        var settings = await GetAsync();
        return settings?.ReportFooter ?? "This document is confidential and intended solely for the use of the individual or entity to whom it is addressed.";
    }
}