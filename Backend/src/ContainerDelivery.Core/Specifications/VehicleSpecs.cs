using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Core.Specifications;

public class VehicleByVinSpec : BaseSpecification<Vehicle>
{
    public VehicleByVinSpec(string vin)
    {
        Criteria = v => v.Vin == vin;
        AddInclude(v => v.Container);
    }
}

public class VehicleByIdSpec : BaseSpecification<Vehicle>
{
    public VehicleByIdSpec(int id)
    {
        Criteria = v => v.Id == id;
        AddInclude(v => v.Container);
        AddInclude(v => v.DeliveredByUser);
    }
}

public class VehiclesByContainerSpec : BaseSpecification<Vehicle>
{
    public VehiclesByContainerSpec(int containerId)
    {
        Criteria = v => v.ContainerId == containerId;
        AddInclude(v => v.Container);
        ApplyOrderBy(v => v.Description);
    }
}

public class UndeliveredVehiclesByContainerSpec : BaseSpecification<Vehicle>
{
    public UndeliveredVehiclesByContainerSpec(int containerId)
    {
        Criteria = v => v.ContainerId == containerId && !v.IsDelivered;
        AddInclude(v => v.Container);
        ApplyOrderBy(v => v.Description);
    }
}

public class DeliveredVehiclesByContainerSpec : BaseSpecification<Vehicle>
{
    public DeliveredVehiclesByContainerSpec(int containerId)
    {
        Criteria = v => v.ContainerId == containerId && v.IsDelivered;
        AddInclude(v => v.Container);
        AddInclude(v => v.DeliveredByUser);
        ApplyOrderByDescending(v => v.DeliveredAt!);
    }
}

public class VehiclesSearchSpec : BaseSpecification<Vehicle>
{
    public VehiclesSearchSpec(string query)
    {
        var term = query.ToLower();
        Criteria = v => v.Vin.ToLower().Contains(term) 
                     || v.Description.ToLower().Contains(term)
                     || v.Container.ContainerNumber.ToLower().Contains(term);
        
        AddInclude(v => v.Container);
        AddInclude(v => v.DeliveredByUser);
        ApplyOrderByDescending(v => v.CreatedAt);
    }
}

public class VehiclesSearchPagedSpec : BaseSpecification<Vehicle>
{
    public VehiclesSearchPagedSpec(string query, int page, int pageSize)
    {
        var term = query.ToLower();
        Criteria = v => v.Vin.ToLower().Contains(term) 
                     || v.Description.ToLower().Contains(term)
                     || v.Container.ContainerNumber.ToLower().Contains(term);
        
        AddInclude(v => v.Container);
        AddInclude(v => v.DeliveredByUser);
        ApplyOrderByDescending(v => v.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

public class VehiclesSearchCountSpec : BaseSpecification<Vehicle>
{
    public VehiclesSearchCountSpec(string query)
    {
        var term = query.ToLower();
        Criteria = v => v.Vin.ToLower().Contains(term) 
                     || v.Description.ToLower().Contains(term)
                     || v.Container.ContainerNumber.ToLower().Contains(term);
    }
}