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

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = Stopwatch.GetTimestamp();
        
        // Log request
        _logger.LogInformation("HTTP {Method} {Path} started", context.Request.Method, context.Request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            
            var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error :
                          context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(logLevel, 
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed:F2}ms",
                context.Request.Method, 
                context.Request.Path, 
                context.Response.StatusCode, 
                elapsed);
        }
    }
}

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail, errorCode, details) = exception switch
        {
            FluentValidation.ValidationException ve => (StatusCodes.Status400BadRequest, "Validation Failed", ve.Message, "VALIDATION_ERROR", ve.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })),
            ContainerDelivery.Core.Exceptions.EntityNotFoundException enfe => (StatusCodes.Status404NotFound, "Not Found", enfe.Message, enfe.ErrorCode, enfe.Details),
            ContainerDelivery.Core.Exceptions.DuplicateEntityException dee => (StatusCodes.Status409Conflict, "Conflict", dee.Message, dee.ErrorCode, dee.Details),
            ContainerDelivery.Core.Exceptions.UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message, "UNAUTHORIZED", null),
            ContainerDelivery.Core.Exceptions.ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message, "FORBIDDEN", null),
            ContainerDelivery.Core.Exceptions.InvalidOperationException ioe => (StatusCodes.Status400BadRequest, "Bad Request", ioe.Message, ioe.ErrorCode, ioe.Details),
            ContainerDelivery.Core.Exceptions.MfaException mfe => (StatusCodes.Status400BadRequest, "MFA Error", mfe.Message, mfe.ErrorCode, null),
            ContainerDelivery.Core.Exceptions.ImportException ie => (StatusCodes.Status400BadRequest, "Import Error", ie.Message, ie.ErrorCode, ie.Details),
            ContainerDelivery.Core.Exceptions.ConcurrencyException ce => (StatusCodes.Status409Conflict, "Concurrency Error", ce.Message, ce.ErrorCode, ce.Details),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred", "INTERNAL_ERROR", null)
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://tools.ietf.org/html/rfc{statusCode}"
        };

        if (errorCode != null)
            problemDetails.Extensions["errorCode"] = errorCode;
        if (details != null)
            problemDetails.Extensions["details"] = details;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}