using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Infrastructure.Auth;
using ContainerDelivery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserRole = ContainerDelivery.Core.Enums.UserRole;
using UserRoleEntity = ContainerDelivery.Core.Entities.UserRole;

namespace ContainerDelivery.Api.Extensions;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed Roles
        await SeedRolesAsync(context);

        // Seed Admin User
        await SeedAdminUserAsync(context, passwordService);

        // Seed default data
        await context.SaveChangesAsync();
        
        logger.LogInformation("Database seeding completed successfully");
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            var roles = new[]
            {
                new Role { Id = 1, Name = UserRole.Admin, Description = "Full system access including user management, audit logs, and container deletion" },
                new Role { Id = 2, Name = UserRole.DeliveryUser, Description = "Can view assigned containers, confirm deliveries, and generate reports" }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext context, IPasswordService passwordService)
    {
        var adminEmail = "admin@container-delivery.com";
        
        if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
        {
            var adminUser = new User(adminEmail, passwordService.HashPassword("Admin123!"), "System Administrator");
            adminUser.IsMfaEnabled = false;
            adminUser.IsActive = true;

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            // Assign Admin role
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRole.Admin);
            if (adminRole != null)
            {
                var userRole = new UserRoleEntity
                {
                    UserId = adminUser.Id,
                    Role = UserRole.Admin,
                    AssignedByUserId = adminUser.Id
                };
                context.UserRoles.Add(userRole);
            }

            await context.SaveChangesAsync();
        }

        // Seed demo delivery user
        var deliveryEmail = "delivery@container-delivery.com";
        if (!await context.Users.AnyAsync(u => u.Email == deliveryEmail))
        {
            var deliveryUser = new User(deliveryEmail, passwordService.HashPassword("Delivery123!"), "Delivery Operator");
            deliveryUser.IsMfaEnabled = false;
            deliveryUser.IsActive = true;

            context.Users.Add(deliveryUser);
            await context.SaveChangesAsync();

            var deliveryRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRole.DeliveryUser);
            if (deliveryRole != null)
            {
                var userRole = new UserRoleEntity
                {
                    UserId = deliveryUser.Id,
                    Role = UserRole.DeliveryUser,
                    AssignedByUserId = 1 // Admin user
                };
                context.UserRoles.Add(userRole);
            }

            await context.SaveChangesAsync();
        }
    }
}

// Add Role entity for seeding
public class Role
{
    public int Id { get; set; }
    public UserRole Name { get; set; }
    public string? Description { get; set; }
}