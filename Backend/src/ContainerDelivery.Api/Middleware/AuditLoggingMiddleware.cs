using ContainerDelivery.Core.Entities;
using ContainerDelivery.Core.Enums;
using ContainerDelivery.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ContainerDelivery.Api.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var originalBody = context.Response.Body;
        
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);
        }
        finally
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }

        // Log audit for tracked endpoints
        if (ShouldAudit(context.Request.Path, context.Request.Method))
        {
            try
            {
                var auditService = context.RequestServices.GetService<IAuditService>();
                if (auditService != null)
                {
                    var userId = GetUserId(context);
                    var action = GetAuditAction(context.Request.Method, context.Response.StatusCode);
                    var entityType = GetEntityType(context.Request.Path);
                    var entityId = GetEntityId(context.Request.Path);

                    await auditService.LogAsync(new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        EntityType = entityType,
                        EntityId = entityId,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers.UserAgent.ToString(),
                        NewValues = JsonSerializer.Serialize(new
                        {
                            Method = context.Request.Method,
                            Path = context.Request.Path.Value,
                            StatusCode = context.Response.StatusCode,
                            DurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                        })
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log");
            }
        }
    }

    private static bool ShouldAudit(string path, string method)
    {
        if (method == "GET") return false; // Don't audit read operations
        
        return path.StartsWith("/api/v1/containers") ||
               path.StartsWith("/api/v1/vehicles") ||
               path.StartsWith("/api/v1/users") ||
               path.StartsWith("/api/v1/auth") ||
               path.StartsWith("/api/v1/import") ||
               path.StartsWith("/api/v1/reports");
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim?.Value, out var userId) ? userId : null;
    }

    private static AuditAction GetAuditAction(string method, int statusCode)
    {
        if (statusCode >= 400) return AuditAction.Update; // Failed attempts still logged
        
        return method switch
        {
            "POST" => AuditAction.Create,
            "PUT" => AuditAction.Update,
            "PATCH" => AuditAction.Update,
            "DELETE" => AuditAction.Delete,
            _ => AuditAction.Update
        };
    }

    private static EntityType GetEntityType(string path)
    {
        if (path.Contains("/containers")) return EntityType.Container;
        if (path.Contains("/vehicles")) return EntityType.Vehicle;
        if (path.Contains("/users")) return EntityType.User;
        if (path.Contains("/import")) return EntityType.ImportBatch;
        if (path.Contains("/reports")) return EntityType.Report;
        return EntityType.User;
    }

    private static int? GetEntityId(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && int.TryParse(parts[^1], out var id))
            return id;
        return null;
    }
}