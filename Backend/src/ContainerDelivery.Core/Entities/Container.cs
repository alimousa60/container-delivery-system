using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContainerDelivery.Core.Enums;

namespace ContainerDelivery.Core.Entities;

public class Container : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ContainerNumber { get; set; } = string.Empty;

    public int TotalVehicles { get; set; } = 0;

    public int DeliveredVehicles { get; set; } = 0;

    [Required]
    public ContainerStatus Status { get; set; } = ContainerStatus.NotStarted;

    [Required]
    public int CreatedByUserId { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public virtual ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();

    public virtual ICollection<ContainerReport> Reports { get; set; } = new List<ContainerReport>();

    public virtual ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();

    // Computed properties
    [NotMapped]
    public int UndeliveredVehicles => TotalVehicles - DeliveredVehicles;

    [NotMapped]
    public double CompletionPercentage => TotalVehicles > 0 
        ? Math.Round((double)DeliveredVehicles / TotalVehicles * 100, 2) 
        : 0;

    [NotMapped]
    public bool CanBeDeleted => Status == ContainerStatus.FullyDelivered;

    [NotMapped]
    public bool HasPendingDeliveries => DeliveredVehicles < TotalVehicles;

    public void RecalculateTotals()
    {
        TotalVehicles = Vehicles.Count;
        DeliveredVehicles = Vehicles.Count(v => v.IsDelivered);
        
        if (DeliveredVehicles == 0)
        {
            Status = ContainerStatus.NotStarted;
            StartedAt = null;
            CompletedAt = null;
        }
        else if (DeliveredVehicles >= TotalVehicles)
        {
            Status = ContainerStatus.FullyDelivered;
            if (CompletedAt == null)
                CompletedAt = DateTime.UtcNow;
            if (StartedAt == null)
                StartedAt = DateTime.UtcNow;
        }
        else
        {
            Status = ContainerStatus.InProgress;
            if (StartedAt == null)
                StartedAt = DateTime.UtcNow;
            CompletedAt = null;
        }
    }

    public void AddVehicle(Vehicle vehicle)
    {
        Vehicles.Add(vehicle);
        TotalVehicles = Vehicles.Count;
    }

    public void RemoveVehicle(Vehicle vehicle)
    {
        Vehicles.Remove(vehicle);
        TotalVehicles = Vehicles.Count;
        DeliveredVehicles = Vehicles.Count(v => v.IsDelivered);
        RecalculateTotals();
    }
}