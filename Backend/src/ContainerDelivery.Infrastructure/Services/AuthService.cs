using Microsoft.Extensions.Logging;
using ContainerDelivery.Application.DTOs;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Exceptions;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;
using ContainerDelivery.Infrastructure.Auth;
using System.Security.Cryptography;
using UserRole = ContainerDelivery.Core.Entities.UserRole;

namespace ContainerDelivery.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMfaService _mfaService;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    private readonly Dictionary<string, (string Token, DateTime Expiry, int UserId)> _passwordResetTokens = new();
    private readonly Dictionary<string, (string RefreshToken, DateTime Expiry, int UserId, string? DeviceId)> _refreshTokens = new();

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IMfaService mfaService,
        IPasswordService passwordService,
        IEmailService emailService,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _mfaService = mfaService;
        _passwordService = passwordService;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string ipAddress, string userAgent, bool rememberDevice = false)
    {
        var spec = new UserByEmailSpec(email);
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(spec);

        if (user == null)
        {
            await _auditService.LogAsync(new AuditLog
            {
                UserId = null,
                Action = AuditAction.Login,
                EntityType = EntityType.User,
                EntityId = null,
                OldValues = null,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Email = email, Success = false, Reason = "User not found" }),
                IpAddress = ipAddress,
                UserAgent = userAgent
            });
            
            return new AuthResult(false, Error: "Invalid credentials");
        }

        if (!user.IsActive)
        {
            return new AuthResult(false, Error: "Account is deactivated");
        }

        if (user.IsLockedOut)
        {
            return new AuthResult(false, Error: "Account is temporarily locked. Try again later.");
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _unitOfWork.Users.UpdateAsync(user);

            await _auditService.LogAsync(new AuditLog
            {
                UserId = user.Id,
                Action = AuditAction.Login,
                EntityType = EntityType.User,
                EntityId = user.Id,
                OldValues = null,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Success = false, Reason = "Invalid password" }),
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            return new AuthResult(false, Error: "Invalid credentials");
        }

        if (user.IsMfaEnabled)
        {
            await _auditService.LogAsync(new AuditLog
            {
                UserId = user.Id,
                Action = AuditAction.Login,
                EntityType = EntityType.User,
                EntityId = user.Id,
                OldValues = null,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Success = true, RequiresMfa = true }),
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            return new AuthResult(false, RequiresMfa: true, User: user);
        }

        user.RecordSuccessfulLogin();
        await _unitOfWork.Users.UpdateAsync(user);

        var roles = await _unitOfWork.UserRoles.ListAsync(new UserRolesByUserSpec(user.Id));
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles.Select(r => r.Role).ToList());
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        StoreRefreshToken(refreshToken, user.Id, rememberDevice ? Guid.NewGuid().ToString() : null);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = user.Id,
            Action = AuditAction.Login,
            EntityType = EntityType.User,
            EntityId = user.Id,
            OldValues = null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Success = true }),
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return new AuthResult(
            true,
            accessToken,
            refreshToken,
            15 * 60,
            user
        );
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
        {
            return new AuthResult(false, Error: "Invalid refresh token");
        }

        if (tokenInfo.Expiry < DateTime.UtcNow)
        {
            _refreshTokens.Remove(refreshToken);
            return new AuthResult(false, Error: "Refresh token expired");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(tokenInfo.UserId);
        if (user == null || !user.IsActive)
        {
            _refreshTokens.Remove(refreshToken);
            return new AuthResult(false, Error: "User not found or inactive");
        }

        var roles = await _unitOfWork.UserRoles.ListAsync(new UserRolesByUserSpec(user.Id));
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles.Select(r => r.Role).ToList());
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        _refreshTokens.Remove(refreshToken);
        StoreRefreshToken(newRefreshToken, user.Id, tokenInfo.DeviceId);

        return new AuthResult(
            true,
            newAccessToken,
            newRefreshToken,
            15 * 60,
            user
        );
    }

    public async Task LogoutAsync(string refreshToken)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
        {
            _refreshTokens.Remove(refreshToken);
            
            await _auditService.LogAsync(new AuditLog
            {
                UserId = tokenInfo.UserId,
                Action = AuditAction.Logout,
                EntityType = EntityType.User,
                EntityId = tokenInfo.UserId,
                OldValues = null,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { }),
                IpAddress = null,
                UserAgent = null
            });
        }
    }

    public async Task<MfaSetupResult> SetupMfaAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new EntityNotFoundException("User", userId);

        var result = _mfaService.GenerateSecret(user.Email);
        user.EnableMfa(result.Secret, string.Join(",", result.RecoveryCodes));
        await _unitOfWork.Users.UpdateAsync(user);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.MfaSetup,
            EntityType = EntityType.User,
            EntityId = userId,
            OldValues = null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { user.IsMfaEnabled }),
            IpAddress = null,
            UserAgent = null
        });

        return new MfaSetupResult(result.Secret, result.QrCodeUrl, result.RecoveryCodes);
    }

    public async Task<AuthResult> VerifyMfaAsync(int userId, string code, string ipAddress, string userAgent, bool rememberDevice = false)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || !user.IsMfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
        {
            return new AuthResult(false, Error: "MFA not configured");
        }

        var isValid = _mfaService.VerifyCode(user.MfaSecret, code);
        if (!isValid)
        {
            await _auditService.LogAsync(new AuditLog
            {
                UserId = userId,
                Action = AuditAction.Login,
                EntityType = EntityType.User,
                EntityId = userId,
                OldValues = null,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Success = false, Reason = "Invalid MFA code" }),
                IpAddress = ipAddress,
                UserAgent = userAgent
            });
            
            return new AuthResult(false, Error: "Invalid MFA code");
        }

        user.RecordSuccessfulLogin();
        await _unitOfWork.Users.UpdateAsync(user);

        var roles = await _unitOfWork.UserRoles.ListAsync(new UserRolesByUserSpec(user.Id));
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles.Select(r => r.Role).ToList());
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var deviceId = rememberDevice ? Guid.NewGuid().ToString() : null;
        StoreRefreshToken(refreshToken, user.Id, deviceId);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Login,
            EntityType = EntityType.User,
            EntityId = userId,
            OldValues = null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Success = true, MfaVerified = true }),
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return new AuthResult(
            true,
            accessToken,
            refreshToken,
            15 * 60,
            user
        );
    }

    public async Task DisableMfaAsync(int userId, string password, string code)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new EntityNotFoundException("User", userId);

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedException("Invalid password");

        if (!_mfaService.VerifyCode(user.MfaSecret!, code))
            throw new MfaException("Invalid MFA code");

        user.DisableMfa();
        await _unitOfWork.Users.UpdateAsync(user);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.MfaDisable,
            EntityType = EntityType.User,
            EntityId = userId,
            OldValues = null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { user.IsMfaEnabled }),
            IpAddress = null,
            UserAgent = null
        });
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new EntityNotFoundException("User", userId);

        if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        var newHash = _passwordService.HashPassword(newPassword);
        user.ChangePassword(newHash);
        await _unitOfWork.Users.UpdateAsync(user);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.Update,
            EntityType = EntityType.User,
            EntityId = userId,
            OldValues = null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Changed = true }),
            IpAddress = null,
            UserAgent = null
        });

        return true;
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email)
    {
        var spec = new UserByEmailSpec(email);
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(spec);

        if (user == null)
        {
            // Don't reveal if user exists
            return new ForgotPasswordResult(true);
        }

        var token = GenerateResetToken();
        _passwordResetTokens[token] = (token, DateTime.UtcNow.AddHours(1), user.Id);

        // In production, send email with reset link
        // await _emailService.SendPasswordResetAsync(user.Email, $"https://app.container-delivery.com/reset-password?token={token}");

        await _auditService.LogAsync(new AuditLog
        {
            UserId = user.Id,
            Action = AuditAction.Update,
            EntityType = EntityType.User,
            EntityId = user.Id,
            OldValues = null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { ResetRequested = true }),
            IpAddress = null,
            UserAgent = null
        });

        return new ForgotPasswordResult(true);
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword)
    {
        if (!_passwordResetTokens.TryGetValue(token, out var tokenInfo))
        {
            return new ResetPasswordResult(false, Error: "Invalid or expired reset token");
        }

        if (tokenInfo.Expiry < DateTime.UtcNow)
        {
            _passwordResetTokens.Remove(token);
            return new ResetPasswordResult(false, Error: "Reset token expired");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(tokenInfo.UserId);
        if (user == null)
        {
            return new ResetPasswordResult(false, Error: "User not found");
        }

        var newHash = _passwordService.HashPassword(newPassword);
        user.ChangePassword(newHash);
        await _unitOfWork.Users.UpdateAsync(user);

        _passwordResetTokens.Remove(token);

        // Invalidate all refresh tokens for this user
        var userTokens = _refreshTokens.Where(kvp => kvp.Value.UserId == user.Id).Select(kvp => kvp.Key).ToList();
        foreach (var rt in userTokens)
            _refreshTokens.Remove(rt);

        await _auditService.LogAsync(new AuditLog
        {
            UserId = user.Id,
            Action = AuditAction.Update,
            EntityType = EntityType.User,
            EntityId = user.Id,
            OldValues = null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { PasswordReset = true }),
            IpAddress = null,
            UserAgent = null
        });

        return new ResetPasswordResult(true);
    }

    private UserDto MapToUserDto(User user)
    {
        var roles = user.Roles.Select(r => r.ToString()).ToArray();
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            IsMfaEnabled = user.IsMfaEnabled
        };
    }

    private void StoreRefreshToken(string refreshToken, int userId, string? deviceId)
    {
        var expiryDays = deviceId != null ? 30 : 7; // Longer expiry for remembered devices
        _refreshTokens[refreshToken] = (refreshToken, DateTime.UtcNow.AddDays(expiryDays), userId, deviceId);
    }

    private string GenerateResetToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
