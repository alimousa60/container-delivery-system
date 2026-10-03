using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Api.Models;

public record AuditLogDto
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string? UserEmail { get; init; }
    public AuditAction Action { get; init; }
    public EntityType EntityType { get; init; }
    public int? EntityId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
}