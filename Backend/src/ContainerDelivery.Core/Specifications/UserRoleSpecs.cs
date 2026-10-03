using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Core.Specifications;

public class UserRolesByUserSpec : BaseSpecification<UserRole>
{
    public UserRolesByUserSpec(int userId)
    {
        Criteria = ur => ur.UserId == userId;
        AddInclude(ur => ur.Role);
    }
}

public class UserRoleByIdSpec : BaseSpecification<UserRole>
{
    public UserRoleByIdSpec(int userId, int roleId)
    {
        Criteria = ur => ur.UserId == userId && ur.RoleId == roleId;
    }
}