using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserRoleEnum = ContainerDelivery.Core.Enums.UserRole;

namespace ContainerDelivery.Core.Entities;

public class UserRole : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public UserRoleEnum Role { get; set; }

    [Required]
    public int AssignedByUserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(AssignedByUserId))]
    public virtual User AssignedByUser { get; set; } = null!;
}