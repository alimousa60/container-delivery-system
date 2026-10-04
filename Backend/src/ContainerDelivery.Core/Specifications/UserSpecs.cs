using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using UserRole = ContainerDelivery.Core.Enums.UserRole;

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
        var term = string.IsNullOrWhiteSpace(search) ? null : search.ToLower();
        var roleValue = role;
        var activeValue = isActive;

        Criteria = u =>
            (term == null || u.Email.ToLower().Contains(term) || u.FullName.ToLower().Contains(term)) &&
            (roleValue == null || u.UserRoles.Any(ur => ur.Role == roleValue.Value)) &&
            (activeValue == null || u.IsActive == activeValue.Value);

        ApplyOrderByDescending(u => u.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
        AddInclude(u => u.UserRoles);
    }
}

public class UsersCountSpec : BaseSpecification<User>
{
    public UsersCountSpec(string? search = null, UserRole? role = null, bool? isActive = null)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.ToLower();
        var roleValue = role;
        var activeValue = isActive;

        Criteria = u =>
            (term == null || u.Email.ToLower().Contains(term) || u.FullName.ToLower().Contains(term)) &&
            (roleValue == null || u.UserRoles.Any(ur => ur.Role == roleValue.Value)) &&
            (activeValue == null || u.IsActive == activeValue.Value);
    }
}
