using System.Collections.Concurrent;

namespace ContainerDelivery.Infrastructure.Auth;

/// <summary>
/// Application-wide in-memory token storage. Registered as a singleton so that
/// scoped services (AuthService) share refresh tokens and password reset tokens
/// across HTTP requests.
/// </summary>
public class TokenStore
{
    public ConcurrentDictionary<string, (string Token, DateTime Expiry, int UserId)> PasswordResetTokens { get; } = new();

    public ConcurrentDictionary<string, (string RefreshToken, DateTime Expiry, int UserId, string? DeviceId)> RefreshTokens { get; } = new();
}
