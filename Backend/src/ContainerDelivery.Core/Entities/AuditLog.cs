using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public AuditAction Action { get; set; }

    [Required]
    [MaxLength(100)]
    public EntityType EntityType { get; set; }

    public int? EntityId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? OldValues { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NewValues { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    public static AuditLog Create<T>(int? userId, AuditAction action, EntityType entityType, int? entityId, T? oldEntity, T? newEntity, string? ipAddress, string? userAgent)
    {
        var oldJson = oldEntity != null ? System.Text.Json.JsonSerializer.Serialize(oldEntity) : null;
        var newJson = newEntity != null ? System.Text.Json.JsonSerializer.Serialize(newEntity) : null;

        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldJson,
            NewValues = newJson,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
}