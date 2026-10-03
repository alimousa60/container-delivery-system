using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using UserRole = ContainerDelivery.Core.Entities.UserRole;

namespace ContainerDelivery.Core.Specifications;

public class VehiclesCountSpec : BaseSpecification<Vehicle>
{
    public VehiclesCountSpec()
    {
        Criteria = v => true;
    }
}

public class VehiclesByDeliveredStatusSpec : BaseSpecification<Vehicle>
{
    public VehiclesByDeliveredStatusSpec(bool isDelivered)
    {
        Criteria = v => v.IsDelivered == isDelivered;
    }
}

public class DeliveryRecordsByContainerSpec : BaseSpecification<DeliveryRecord>
{
    public DeliveryRecordsByContainerSpec(int containerId)
    {
        Criteria = dr => dr.ContainerId == containerId;
        AddInclude(dr => dr.Vehicle);
        AddInclude(dr => dr.DeliveredByUser);
        ApplyOrderByDescending(dr => dr.CreatedAt);
    }
}

public class DeliveryRecordsByVehicleSpec : BaseSpecification<DeliveryRecord>
{
    public DeliveryRecordsByVehicleSpec(int vehicleId)
    {
        Criteria = dr => dr.VehicleId == vehicleId;
        AddInclude(dr => dr.DeliveredByUser);
        ApplyOrderByDescending(dr => dr.CreatedAt);
    }
}

public class AuditLogsPagedSpec : BaseSpecification<AuditLog>
{
    public AuditLogsPagedSpec(int page, int pageSize, int? userId = null, AuditAction? action = null, EntityType? entityType = null, int? entityId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        Criteria = a => true;

        if (userId.HasValue)
        {
            Criteria = a => a.UserId == userId.Value;
        }

        if (action.HasValue)
        {
            var actionCriteria = Criteria;
            Criteria = a => actionCriteria(a) && a.Action == action.Value;
        }

        if (entityType.HasValue)
        {
            var entityCriteria = Criteria;
            Criteria = a => entityCriteria(a) && a.EntityType == entityType.Value;
        }

        if (entityId.HasValue)
        {
            var entityIdCriteria = Criteria;
            Criteria = a => entityIdCriteria(a) && a.EntityId == entityId.Value;
        }

        if (fromDate.HasValue)
        {
            var dateCriteria = Criteria;
            Criteria = a => dateCriteria(a) && a.CreatedAt >= fromDate.Value;
        }

        if (toDate.HasValue)
        {
            var dateCriteria = Criteria;
            Criteria = a => dateCriteria(a) && a.CreatedAt <= toDate.Value;
        }

        ApplyOrderByDescending(a => a.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AddInclude(a => a.User);
    }
}

public class AuditLogsCountSpec : BaseSpecification<AuditLog>
{
    public AuditLogsCountSpec(int? userId = null, AuditAction? action = null, EntityType? entityType = null, int? entityId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        Criteria = a => true;

        if (userId.HasValue)
        {
            Criteria = a => a.UserId == userId.Value;
        }

        if (action.HasValue)
        {
            var actionCriteria = Criteria;
            Criteria = a => actionCriteria(a) && a.Action == action.Value;
        }

        if (entityType.HasValue)
        {
            var entityCriteria = Criteria;
            Criteria = a => entityCriteria(a) && a.EntityType == entityType.Value;
        }

        if (entityId.HasValue)
        {
            var entityIdCriteria = Criteria;
            Criteria = a => entityIdCriteria(a) && a.EntityId == entityId.Value;
        }

        if (fromDate.HasValue)
        {
            var dateCriteria = Criteria;
            Criteria = a => dateCriteria(a) && a.CreatedAt >= fromDate.Value;
        }

        if (toDate.HasValue)
        {
            var dateCriteria = Criteria;
            Criteria = a => dateCriteria(a) && a.CreatedAt <= toDate.Value;
        }
    }
}

public class AuditLogsExportSpec : BaseSpecification<AuditLog>
{
    public AuditLogsExportSpec(DateTime? fromDate = null, DateTime? toDate = null)
    {
        Criteria = a => true;

        if (fromDate.HasValue)
        {
            Criteria = a => a.CreatedAt >= fromDate.Value;
        }

        if (toDate.HasValue)
        {
            var dateCriteria = Criteria;
            Criteria = a => dateCriteria(a) && a.CreatedAt <= toDate.Value;
        }

        ApplyOrderByDescending(a => a.CreatedAt);
        AddInclude(a => a.User);
    }
}

public class ContainerReportsByContainerSpec : BaseSpecification<ContainerReport>
{
    public ContainerReportsByContainerSpec(int containerId)
    {
        Criteria = r => r.ContainerId == containerId;
        ApplyOrderByDescending(r => r.CreatedAt);
        AddInclude(r => r.GeneratedByUser);
    }
}

public class ContainerReportByIdSpec : BaseSpecification<ContainerReport>
{
    public ContainerReportByIdSpec(int id)
    {
        Criteria = r => r.Id == id;
        AddInclude(r => r.Container);
        AddInclude(r => r.GeneratedByUser);
    }
}

public class ImportBatchesPagedSpec : BaseSpecification<ImportBatch>
{
    public ImportBatchesPagedSpec(int page, int pageSize)
    {
        ApplyOrderByDescending(i => i.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AddInclude(i => i.ImportedByUser);
    }
}

public class ImportBatchesCountSpec : BaseSpecification<ImportBatch>
{
    public ImportBatchesCountSpec()
    {
        Criteria = i => true;
    }
}

public class UserRolesByUserSpec : BaseSpecification<UserRole>
{
    public UserRolesByUserSpec(int userId)
    {
        Criteria = ur => ur.UserId == userId;
        AddInclude(ur => ur.AssignedByUser);
    }
}

public class UserRoleByIdSpec : BaseSpecification<UserRole>
{
    public UserRoleByIdSpec(int userId, ContainerDelivery.Core.Enums.UserRole role)
    {
        Criteria = ur => ur.UserId == userId && ur.Role == role;
    }
}