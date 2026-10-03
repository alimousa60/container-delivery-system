using ContainerDelivery.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ContainerDelivery.Api.Middleware;

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
            _logger.LogError(ex, "Unhandled exception occurred");
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

        var problemDetails = new ProblemDetails
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