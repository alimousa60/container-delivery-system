using System.Security.Claims;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using UserRole = ContainerDelivery.Core.Enums.UserRole;

namespace ContainerDelivery.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, string ipAddress, string userAgent, bool rememberDevice = false);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent);
    Task LogoutAsync(string refreshToken);
    Task<MfaSetupResult> SetupMfaAsync(int userId);
    Task<AuthResult> VerifyMfaAsync(int userId, string code, string ipAddress, string userAgent, bool rememberDevice = false);
    Task DisableMfaAsync(int userId, string password, string code);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<ForgotPasswordResult> ForgotPasswordAsync(string email);
    Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword);
}

public record AuthResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    int? ExpiresIn = null,
    User? User = null,
    string? Error = null,
    bool RequiresMfa = false
);

public record MfaSetupResult(
    string Secret,
    string QrCodeUrl,
    string[] RecoveryCodes
);

public record ForgotPasswordResult(bool Success, string? Error = null);

public record ResetPasswordResult(bool Success, string? Error = null);

public interface IUserService
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? search = null, UserRole? role = null, bool? isActive = null);
    Task<User> CreateAsync(string email, string password, string fullName, string? phoneNumber, IEnumerable<UserRole> roles, int createdByUserId);
    Task<User> UpdateAsync(int id, string fullName, string? phoneNumber, bool isActive, int updatedByUserId);
    Task AssignRoleAsync(int userId, UserRole role, int assignedByUserId);
    Task RemoveRoleAsync(int userId, UserRole role, int removedByUserId);
    Task<bool> DeleteAsync(int id, int deletedByUserId);
    Task<User?> ValidateCredentialsAsync(string email, string password);
    Task UpdateLastLoginAsync(int userId);
    Task IncrementFailedLoginAsync(int userId);
    Task ResetFailedLoginsAsync(int userId);
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public interface IContainerService
{
    Task<Container?> GetByIdAsync(int id);
    Task<Container?> GetByNumberAsync(string containerNumber);
    Task<PagedResult<Container>> GetPagedAsync(int page, int pageSize, ContainerStatus? status = null, string? search = null, string? sortBy = null, string? sortOrder = null);
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<Container> CreateAsync(string containerNumber, int createdByUserId, string? notes = null);
    Task<Container> UpdateAsync(int id, string? notes, int updatedByUserId);
    Task<bool> DeleteAsync(int id, int deletedByUserId);
    Task<Container> StartDeliveryAsync(int containerId, int userId);
    Task<Container> RecalculateTotalsAsync(int containerId);
    Task<Container> CloseContainerAsync(int containerId, int userId);
    Task<Container> ReopenContainerAsync(int containerId, int userId);
    Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(int containerId);
    Task<IReadOnlyList<Vehicle>> GetUndeliveredVehiclesAsync(int containerId);
    Task<IReadOnlyList<Vehicle>> GetDeliveredVehiclesAsync(int containerId);
    Task<ImportResultDto> ImportAsync(Stream excelStream, string fileName, int importedByUserId, string? containerNumberPrefix = null);
    Task<ImportBatchStatusDto> GetImportStatusAsync(int importBatchId);
    Task<PagedResult<Container>> GetArchiveAsync(int page, int pageSize, string? search = null);
}

public record DashboardStats(
    int TotalContainers,
    int TotalVehicles,
    int PendingContainers,
    int CompletedContainers,
    int InProgressContainers,
    int DeliveredVehicles,
    int UndeliveredVehicles
);

public interface IVehicleService
{
    Task<Vehicle?> GetByIdAsync(int id);
    Task<Vehicle?> GetByVinAsync(string vin);
    Task<PagedResult<Vehicle>> SearchAsync(string query, int page, int pageSize);
    Task<DeliveryResult> DeliverAsync(string vin, int containerId, int userId, DeliveryMethod method, string? notes = null, string? scannedVin = null);
    Task<BatchDeliveryResult> DeliverBatchAsync(int containerId, IEnumerable<BatchDeliveryItem> items, int userId);
    Task<Vehicle> UndeliverAsync(int vehicleId, int userId, string? reason = null);
}

public record DeliveryResult(
    bool Success,
    Vehicle? Vehicle = null,
    Container? Container = null,
    string? Warning = null,
    string? Error = null
);

public record BatchDeliveryItem(
    string Vin,
    DeliveryMethod Method,
    string? ScannedVin = null,
    string? Notes = null
);

public record BatchDeliveryResult(
    int SuccessCount,
    int FailureCount,
    IEnumerable<BatchDeliveryError> Errors
);

public record BatchDeliveryError(
    string Vin,
    string Error
);

public interface IImportService
{
    Task<ImportResultDto> ImportAsync(Stream excelStream, string fileName, int importedByUserId, string? containerNumberPrefix = null);
    Task<ImportBatch> GetByIdAsync(int id);
    Task<PagedResult<ImportBatch>> GetPagedAsync(int page, int pageSize);
}

public class ImportResultDto
{
    public int ImportBatchId { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int DuplicateVins { get; set; }
    public int ContainersCreated { get; set; }
    public int VehiclesImported { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<ImportErrorDto> Errors { get; set; } = [];
    public TimeSpan ProcessingTime { get; set; }
}

public record ImportBatchStatusDto(
    int Id = 0,
    string FileName = "",
    ImportStatus Status = default,
    int TotalRecords = 0,
    int SuccessfulRecords = 0,
    int FailedRecords = 0,
    DateTime ImportedAt = default,
    DateTime? CompletedAt = null,
    List<ImportErrorDto>? Errors = null
);

public record ImportErrorDto(
    int Row = 0,
    string Field = "",
    string Value = "",
    string Error = ""
);

public interface IReportService
{
    Task<ContainerReport> GenerateAsync(int containerId, int generatedByUserId);
    Task<ContainerReport> GetByIdAsync(int id);
    Task<IReadOnlyList<ContainerReport>> GetByContainerIdAsync(int containerId);
    Task<PagedResult<ContainerReport>> GetPagedAsync(int page, int pageSize);
    Task<bool> DeleteAsync(int reportId, int userId);
    Task<Stream> DownloadAsync(int reportId);
    Task<BulkReportResult> GenerateBulkAsync(IEnumerable<int> containerIds, int generatedByUserId);
}

public record BulkReportResult(
    int SuccessCount,
    int FailureCount,
    IEnumerable<BulkReportError> Errors
);

public record BulkReportError(
    int ContainerId,
    string Error
);

public interface IAuditService
{
    Task LogAsync(AuditLog auditLog);
    Task<PagedResult<AuditLog>> GetPagedAsync(int page, int pageSize, int? userId = null, AuditAction? action = null, EntityType? entityType = null, int? entityId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Stream> ExportAsync(DateTime? fromDate = null, DateTime? toDate = null);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = true);
    Task SendPasswordResetAsync(string to, string resetLink);
    Task SendMfaSetupAsync(string to, string secret, string qrCodeUrl, string[] recoveryCodes);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<UserRole> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    int? GetUserIdFromToken(string token);
}

public interface IMfaService
{
    MfaSetupResult GenerateSecret(string userEmail);
    bool VerifyCode(string base32Secret, string code, TimeSpan? tolerance = null);
    string[] GenerateRecoveryCodes(int count = 10);
    bool VerifyRecoveryCode(string[] recoveryCodes, string code);
    string[] RemoveUsedRecoveryCode(string[] recoveryCodes, string usedCode);
}

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    bool NeedsRehash(string hash);
}

public interface ISettingsService
{
    Task<CompanySettings?> GetAsync();
    Task<CompanySettings> UpdateAsync(CompanySettings settings, int updatedByUserId);
    Task<string?> GetLogoUrlAsync();
    Task<string> GetCompanyNameAsync();
    Task<string> GetReportFooterAsync();
}