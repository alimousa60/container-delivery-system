using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Core.Specifications;

public class UserByEmailSpec : BaseSpecification<User>
{
    public UserByEmailSpec(string email)
    {
        Criteria = u => u.Email == email;
    }
}

public class UserByIdSpec : BaseSpecification<User>
{
    public UserByIdSpec(int id)
    {
        Criteria = u => u.Id == id;
        AddInclude(u => u.UserRoles);
    }
}

public class UsersPagedSpec : BaseSpecification<User>
{
    public UsersPagedSpec(int page, int pageSize, string? search = null, UserRole? role = null, bool? isActive = null)
    {
        Criteria = u => true;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            Criteria = u => u.Email.ToLower().Contains(term) || u.FullName.ToLower().Contains(term);
        }

        if (role.HasValue)
        {
            var roleId = (int)role.Value;
            Criteria = u => u.UserRoles.Any(ur => ur.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            var activeCriteria = Criteria;
            Criteria = u => activeCriteria(u) && u.IsActive == isActive.Value;
        }

        ApplyOrderByDescending(u => u.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AddInclude(u => u.UserRoles);
    }
}

public class UsersCountSpec : BaseSpecification<User>
{
    public UsersCountSpec(string? search = null, UserRole? role = null, bool? isActive = null)
    {
        Criteria = u => true;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            Criteria = u => u.Email.ToLower().Contains(term) || u.FullName.ToLower().Contains(term);
        }

        if (role.HasValue)
        {
            var roleId = (int)role.Value;
            Criteria = u => u.UserRoles.Any(ur => ur.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            var activeCriteria = Criteria;
            Criteria = u => activeCriteria(u) && u.IsActive == isActive.Value;
        }
    }
}