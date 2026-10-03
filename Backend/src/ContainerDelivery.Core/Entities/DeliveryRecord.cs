using Ardalis.GuardClauses;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class DeliveryRecord : BaseEntity
{
    public int VehicleId { get; private set; }
    public int ContainerId { get; private set; }
    public int DeliveredByUserId { get; private set; }
    public DeliveryMethod DeliveryMethod { get; private set; }
    public string? Notes { get; private set; }
    public string? ScannedVin { get; private set; }

    // Navigation properties
    public Vehicle Vehicle { get; private set; } = null!;
    public Container Container { get; private set; } = null!;
    public User DeliveredByUser { get; private set; } = null!;

    private DeliveryRecord() { } // EF Core

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