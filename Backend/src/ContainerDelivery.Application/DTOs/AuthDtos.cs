using System.ComponentModel.DataAnnotations;

namespace ContainerDelivery.Application.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

public class VerifyMfaRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "MFA code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "MFA code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "MFA code must be 6 digits")]
    public string Code { get; set; } = string.Empty;

    public bool RememberDevice { get; set; } = false;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class DisableMfaRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "MFA code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "MFA code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "MFA code must be 6 digits")]
    public string Code { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", ErrorMessage = "Password must contain uppercase, lowercase, number and special character")]
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Reset token is required")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", ErrorMessage = "Password must contain uppercase, lowercase, number and special character")]
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int? ExpiresIn { get; set; }
    public bool RequiresMfa { get; set; }
    public UserDto? User { get; set; }
    public string? Error { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public bool IsMfaEnabled { get; set; }
}