using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Application.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public AuditAction Action { get; set; }
    public EntityType EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}