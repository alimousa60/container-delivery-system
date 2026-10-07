using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class Vehicle : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Vin { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int ContainerId { get; set; }

    public bool IsDelivered { get; set; } = false;

    public DateTime? DeliveredAt { get; set; }

    public int? DeliveredByUserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ContainerId))]
    public virtual Container Container { get; set; } = null!;

    [ForeignKey(nameof(DeliveredByUserId))]
    public virtual User? DeliveredByUser { get; set; }

    public virtual ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();

    public Vehicle()
    {
    }

    public Vehicle(string vin, string description, int containerId)
    {
        Vin = vin;
        Description = description;
        ContainerId = containerId;
    }

    public void MarkAsDelivered(int userId, DeliveryMethod method = DeliveryMethod.Manual, string? notes = null, string? scannedVin = null)
    {
        if (IsDelivered)
            throw new InvalidOperationException("Vehicle is already delivered");

        IsDelivered = true;
        DeliveredAt = DateTime.UtcNow;
        DeliveredByUserId = userId;

        var record = new DeliveryRecord
        {
            VehicleId = Id,
            ContainerId = ContainerId,
            DeliveredByUserId = userId,
            DeliveryMethod = method,
            Notes = notes,
            ScannedVin = scannedVin
        };

        DeliveryRecords.Add(record);
        Container.DeliveryRecords.Add(record);
    }

    public void MarkAsUndelivered(int userId, string? reason = null)
    {
        if (!IsDelivered)
            throw new InvalidOperationException("Vehicle is not delivered");

        IsDelivered = false;
        DeliveredAt = null;
        DeliveredByUserId = null;

        var record = new DeliveryRecord
        {
            VehicleId = Id,
            ContainerId = ContainerId,
            DeliveredByUserId = userId,
            DeliveryMethod = DeliveryMethod.Manual,
            Notes = $"Undelivered: {reason}",
            ScannedVin = null
        };

        DeliveryRecords.Add(record);
        Container.DeliveryRecords.Add(record);
    }

    public bool ValidateVin(string scannedVin)
    {
        return Vin.Equals(scannedVin, StringComparison.OrdinalIgnoreCase);
    }
}