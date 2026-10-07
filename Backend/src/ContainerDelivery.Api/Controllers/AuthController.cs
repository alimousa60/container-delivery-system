using ContainerDelivery.Api.Models;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ContainerDelivery.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("Auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// User login with email and password. Returns JWT tokens or requires MFA.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress, userAgent, request.RememberMe);

        if (!result.Success && !result.RequiresMfa)
        {
            return Unauthorized(new { error = result.Error });
        }

        var response = new AuthResponse
        {
            Success = result.Success,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            RequiresMfa = result.RequiresMfa,
            User = result.User != null ? new UserDto
            {
                Id = result.User.Id,
                Email = result.User.Email,
                FullName = result.User.FullName,
                PhoneNumber = result.User.PhoneNumber,
                IsActive = result.User.IsActive,
                Roles = result.User.Roles.Select(r => r.ToString()).ToArray(),
                IsMfaEnabled = result.User.IsMfaEnabled,
                CreatedAt = result.User.CreatedAt,
                LastLoginAt = result.User.LastLoginAt
            } : null,
            Error = result.Error
        };

        return Ok(response);
    }

    /// <summary>
    /// Verify MFA code during login
    /// </summary>
    [HttpPost("mfa/verify")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.VerifyMfaAsync(request.UserId, request.Code, ipAddress, userAgent, request.RememberDevice);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        var response = new AuthResponse
        {
            Success = true,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
            User = result.User != null ? new UserDto
            {
                Id = result.User.Id,
                Email = result.User.Email,
                FullName = result.User.FullName,
                PhoneNumber = result.User.PhoneNumber,
                IsActive = result.User.IsActive,
                Roles = result.User.Roles.Select(r => r.ToString()).ToArray(),
                IsMfaEnabled = result.User.IsMfaEnabled,
                CreatedAt = result.User.CreatedAt,
                LastLoginAt = result.User.LastLoginAt
            } : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress, userAgent);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn
        });
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Setup MFA for current user (generates secret and QR code)
    /// </summary>
    [HttpPost("mfa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(MfaSetupResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetupMfa()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.SetupMfaAsync(userId);

        return Ok(new MfaSetupResponse
        {
            Secret = result.Secret,
            QrCodeUrl = result.QrCodeUrl,
            RecoveryCodes = result.RecoveryCodes
        });
    }

    /// <summary>
    /// Disable MFA for current user
    /// </summary>
    [HttpPost("mfa/disable")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        try
        {
            await _authService.DisableMfaAsync(userId, request.Password, request.Code);
            return Ok(new { message = "MFA disabled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Change password for current user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        
        if (!result)
            return BadRequest(new { error = "Current password is incorrect" });

        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "If the email exists, a reset link has been sent" });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
        
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Password reset successfully" });
    }
}