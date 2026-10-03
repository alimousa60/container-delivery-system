using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ContainerDelivery.Api.Middleware;

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