using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Interfaces;

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