using Ardalis.GuardClauses;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class DeliveryRecord : BaseEntity
{
    public int VehicleId { get; set; }
    public int ContainerId { get; set; }
    public int DeliveredByUserId { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public string? Notes { get; set; }
    public string? ScannedVin { get; set; }

    // Navigation properties
    public Vehicle Vehicle { get; set; } = null!;
    public Container Container { get; set; } = null!;
    public User DeliveredByUser { get; set; } = null!;

    public DeliveryRecord() { } // EF Core

    public DeliveryRecord(int vehicleId, int containerId, int deliveredByUserId, DeliveryMethod method, string? notes = null, string? scannedVin = null)
    {
        VehicleId = vehicleId;
        ContainerId = containerId;
        DeliveredByUserId = deliveredByUserId;
        DeliveryMethod = method;
        Notes = notes;
        ScannedVin = scannedVin;
    }
}