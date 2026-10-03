using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Api.Models;

public record UserDto
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
    public bool IsMfaEnabled { get; init; }
    public string[] Roles { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record CreateUserRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public UserRole[] Roles { get; init; } = [];
}

public record UpdateUserRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
}

public record AssignRoleRequest
{
    public UserRole Role { get; init; }
}

public record AdminResetPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}