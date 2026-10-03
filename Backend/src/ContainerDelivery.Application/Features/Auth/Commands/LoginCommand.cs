using MediatR;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Application.Features.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string IpAddress,
    string UserAgent,
    bool RememberMe = false
) : IRequest<AuthResult>;

public record AuthResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    int? ExpiresIn = null,
    UserDto? User = null,
    string? Error = null,
    bool RequiresMfa = false
);

public record UserDto(
    int Id,
    string Email,
    string FullName,
    string[] Roles,
    bool IsMfaEnabled
);