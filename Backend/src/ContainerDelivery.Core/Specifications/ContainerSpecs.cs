using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Core.Specifications;

public class ContainerByNumberSpec : BaseSpecification<Container>
{
    public ContainerByNumberSpec(string containerNumber)
    {
        Criteria = c => c.ContainerNumber == containerNumber;
    }
}

public class ContainerByIdSpec : BaseSpecification<Container>
{
    public ContainerByIdSpec(int id)
    {
        Criteria = c => c.Id == id;
        AddInclude(c => c.Vehicles);
        AddInclude(c => c.CreatedByUser);
    }
}

public class ContainersPagedSpec : BaseSpecification<Container>
{
    public ContainersPagedSpec(int page, int pageSize, ContainerStatus? status = null, string? search = null, string? sortBy = null, string? sortOrder = null)
    {
        var statusValue = status;
        var term = string.IsNullOrWhiteSpace(search) ? null : search.ToLower();

        Criteria = c =>
            (statusValue == null || c.Status == statusValue.Value) &&
            (term == null || c.ContainerNumber.ToLower().Contains(term));

        ApplySorting(sortBy, sortOrder);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AddInclude(c => c.CreatedByUser);
    }

    private void ApplySorting(string? sortBy, string? sortOrder)
    {
        var isDesc = sortOrder?.ToLower() == "desc";

        switch (sortBy?.ToLower())
        {
            case "containernumber":
                if (isDesc) ApplyOrderByDescending(c => c.ContainerNumber);
                else ApplyOrderBy(c => c.ContainerNumber);
                break;
            case "status":
                if (isDesc) ApplyOrderByDescending(c => c.Status);
                else ApplyOrderBy(c => c.Status);
                break;
            case "createdat":
            default:
                if (isDesc) ApplyOrderByDescending(c => c.CreatedAt);
                else ApplyOrderBy(c => c.CreatedAt);
                break;
        }
    }
}

public class ContainersCountSpec : BaseSpecification<Container>
{
    public ContainersCountSpec(ContainerStatus? status = null, string? search = null)
    {
        var statusValue = status;
        var term = string.IsNullOrWhiteSpace(search) ? null : search.ToLower();

        Criteria = c =>
            (statusValue == null || c.Status == statusValue.Value) &&
            (term == null || c.ContainerNumber.ToLower().Contains(term));
    }
}

public class ContainersWithVehiclesSpec : BaseSpecification<Container>
{
    public ContainersWithVehiclesSpec(int containerId)
    {
        Criteria = c => c.Id == containerId;
        AddInclude(c => c.Vehicles);
        AddInclude(c => c.CreatedByUser);
    }
}

public class ContainersArchiveSpec : BaseSpecification<Container>
{
    public ContainersArchiveSpec(int page, int pageSize, string? search = null)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.ToLower();

        Criteria = c =>
            c.Status == ContainerStatus.Closed &&
            (term == null || c.ContainerNumber.ToLower().Contains(term));

        ApplyOrderByDescending(c => c.ClosedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AddInclude(c => c.CreatedByUser);
        AddInclude(c => c.ClosedByUser);
    }
}

public class ContainersArchiveCountSpec : BaseSpecification<Container>
{
    public ContainersArchiveCountSpec(string? search = null)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.ToLower();

        Criteria = c =>
            c.Status == ContainerStatus.Closed &&
            (term == null || c.ContainerNumber.ToLower().Contains(term));
    }
}
