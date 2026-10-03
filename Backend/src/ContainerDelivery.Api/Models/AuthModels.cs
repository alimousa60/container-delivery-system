namespace ContainerDelivery.Api.Models;

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool RememberMe { get; init; } = false;
}

public record VerifyMfaRequest
{
    public int UserId { get; init; }
    public string Code { get; init; } = string.Empty;
    public bool RememberDevice { get; init; } = false;
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record DisableMfaRequest
{
    public string Password { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}

public record ResetPasswordRequest
{
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public int? ExpiresIn { get; init; }
    public bool RequiresMfa { get; init; }
    public UserDto? User { get; init; }
    public string? Error { get; init; }
}

public record UserDto
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string[] Roles { get; init; } = [];
    public bool IsMfaEnabled { get; init; }
}

public record MfaSetupResponse
{
    public string Secret { get; init; } = string.Empty;
    public string QrCodeUrl { get; init; } = string.Empty;
    public string[] RecoveryCodes { get; init; } = [];
}