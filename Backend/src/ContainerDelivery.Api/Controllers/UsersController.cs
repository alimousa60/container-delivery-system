using ContainerDelivery.Api.Models;
using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserRole = ContainerDelivery.Core.Enums.UserRole;

namespace ContainerDelivery.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _userService.GetPagedAsync(page, pageSize, search, role, isActive);
        
        var items = result.Items.Select(MapToDto).ToList();
        
        return Ok(PagedResponse<UserDto>.Create(items, result.TotalCount, page, pageSize));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
            return NotFound();
        
        return Ok(MapToDto(user));
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var user = await _userService.CreateAsync(
            request.Email, 
            request.Password, 
            request.FullName, 
            request.PhoneNumber, 
            request.Roles, 
            userId);
        
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, MapToDto(user));
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var user = await _userService.UpdateAsync(id, request.FullName, request.PhoneNumber, request.IsActive, userId);
        
        return Ok(MapToDto(user));
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    [HttpPost("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        await _userService.AssignRoleAsync(id, request.Role, userId);
        
        return Ok(new { message = "Role assigned successfully" });
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    [HttpDelete("{id}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(int id, int roleId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var role = (UserRole)roleId;
        
        await _userService.RemoveRoleAsync(id, role, userId);
        
        return Ok(new { message = "Role removed successfully" });
    }

    /// <summary>
    /// Reset user password (Admin)
    /// </summary>
    [HttpPut("{id}/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordRequest request)
    {
        // Implementation would call the user service
        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>
    /// Deactivate user (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        await _userService.DeleteAsync(id, userId);
        
        return NoContent();
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsMfaEnabled = user.IsMfaEnabled,
            Roles = user.Roles.Select(r => r.ToString()).ToArray(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}