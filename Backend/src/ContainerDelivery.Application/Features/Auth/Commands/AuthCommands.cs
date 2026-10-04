using ContainerDelivery.Application.Common;
using MediatR;

namespace ContainerDelivery.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress,
    string UserAgent
) : IRequest<AuthResult>;

public record LogoutCommand(
    string RefreshToken
) : IRequest<ApiResponse>;

public record SetupMfaCommand(
    int UserId
) : IRequest<MfaSetupResult>;

public record MfaSetupResult(
    string Secret,
    string QrCodeUrl,
    string[] RecoveryCodes
);

public record VerifyMfaCommand(
    int UserId,
    string Code,
    bool RememberDevice = false
) : IRequest<AuthResult>;

public record DisableMfaCommand(
    int UserId,
    string Password,
    string Code
) : IRequest<ApiResponse>;

public record ChangePasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<ApiResponse>;

public record ForgotPasswordCommand(
    string Email
) : IRequest<ForgotPasswordResult>;

public record ForgotPasswordResult(
    bool Success,
    string? Error = null
);

public record ResetPasswordCommand(
    string Token,
    string NewPassword
) : IRequest<ResetPasswordResult>;

public record ResetPasswordResult(
    bool Success,
    string? Error = null
);