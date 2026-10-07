using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Exceptions;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Core.Specifications;
using UserRole = ContainerDelivery.Core.Enums.UserRole;
using UserRoleEntity = ContainerDelivery.Core.Entities.UserRole;

namespace ContainerDelivery.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public UserService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(new UserByIdSpec(id));
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(new UserByEmailSpec(email));
    }

    public async Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, string? search = null, UserRole? role = null, bool? isActive = null)
    {
        var items = await _unitOfWork.Users.ListAsync(new UsersPagedSpec(page, pageSize, search, role, isActive));
        var totalCount = await _unitOfWork.Users.CountAsync(new UsersCountSpec(search, role, isActive));

        return new PagedResult<User>(items, totalCount, page, pageSize);
    }

    public async Task<User> CreateAsync(string email, string password, string fullName, string? phoneNumber, IEnumerable<UserRole> roles, int createdByUserId)
    {
        var existing = await GetByEmailAsync(email);
        if (existing != null)
            throw new DuplicateEntityException("User", "Email", email);

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            PasswordHash = _passwordService.HashPassword(password),
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        foreach (var role in roles.Distinct())
        {
            await _unitOfWork.UserRoles.AddAsync(new UserRoleEntity
            {
                UserId = user.Id,
                Role = role,
                AssignedByUserId = createdByUserId
            });
        }

        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<User> UpdateAsync(int id, string fullName, string? phoneNumber, bool isActive, int updatedByUserId)
    {
        var user = await GetByIdAsync(id)
            ?? throw new EntityNotFoundException("User", id);

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.IsActive = isActive;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task AssignRoleAsync(int userId, UserRole role, int assignedByUserId)
    {
        var user = await GetByIdAsync(userId)
            ?? throw new EntityNotFoundException("User", userId);

        if (user.UserRoles.Any(ur => ur.Role == role))
            return;

        await _unitOfWork.UserRoles.AddAsync(new UserRoleEntity
        {
            UserId = userId,
            Role = role,
            AssignedByUserId = assignedByUserId
        });

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(int userId, UserRole role, int removedByUserId)
    {
        var user = await GetByIdAsync(userId)
            ?? throw new EntityNotFoundException("User", userId);

        var userRole = user.UserRoles.FirstOrDefault(ur => ur.Role == role);
        if (userRole == null)
            return;

        await _unitOfWork.UserRoles.DeleteAsync(userRole);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id, int deletedByUserId)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
            return false;

        await _unitOfWork.Users.DeleteAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);
        if (user == null || !user.IsActive)
            return null;

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return;

        user.RecordSuccessfulLogin();
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task IncrementFailedLoginAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return;

        user.RecordFailedLogin();
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResetFailedLoginsAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return;

        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
}
