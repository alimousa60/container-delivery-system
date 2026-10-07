using ContainerDelivery.Core.Interfaces;
using ContainerDelivery.Infrastructure.Auth;
using ContainerDelivery.Infrastructure.Data;
using ContainerDelivery.Infrastructure.Email;
using ContainerDelivery.Infrastructure.Repositories;
using ContainerDelivery.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContainerDelivery.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                }));

        // Repositories & Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IContainerService, ContainerService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISettingsService, SettingsService>();

        // Infrastructure Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }
}
