using System.ComponentModel.DataAnnotations;
using ContainerDelivery.Core.Entities;

namespace ContainerDelivery.Api.Models;

public record CompanySettingsDto
{
    public int Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? ReportFooter { get; init; }
    public string? Website { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }
    public string? LastModifiedBy { get; init; }
}

public record UpdateSettingsRequest
{
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Company name must be between 1 and 200 characters")]
    public string CompanyName { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Logo URL cannot exceed 500 characters")]
    public string? LogoUrl { get; init; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; init; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
    public string? Phone { get; init; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
    public string? Email { get; init; }

    [StringLength(2000, ErrorMessage = "Report footer cannot exceed 2000 characters")]
    public string? ReportFooter { get; init; }

    [StringLength(100, ErrorMessage = "Website cannot exceed 100 characters")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string? Website { get; init; }

    public bool IsActive { get; init; } = true;
}