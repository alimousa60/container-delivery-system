using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Serilog;

namespace ContainerDelivery.Api.Filters;

public class ValidateModelFilter : IAsyncActionFilter
{
    private readonly ILogger<ValidateModelFilter> _logger;

    public ValidateModelFilter(ILogger<ValidateModelFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            _logger.LogWarning("Model validation failed for {Action}: {Errors}",
                context.ActionDescriptor.DisplayName, 
                string.Join("; ", errors.SelectMany(e => e.Value)));

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred",
                Instance = context.HttpContext.Request.Path
            };

            context.Result = new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }
}

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception occurred in {Path}", context.HttpContext.Request.Path);

        var (statusCode, title, detail, errorCode, details) = context.Exception switch
        {
            FluentValidation.ValidationException ve => (StatusCodes.Status400BadRequest, "Validation Failed", ve.Message, "VALIDATION_ERROR", ve.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })),
            ContainerDelivery.Core.Exceptions.EntityNotFoundException enfe => (StatusCodes.Status404NotFound, "Not Found", enfe.Message, enfe.ErrorCode, enfe.Details),
            ContainerDelivery.Core.Exceptions.DuplicateEntityException dee => (StatusCodes.Status409Conflict, "Conflict", dee.Message, dee.ErrorCode, dee.Details),
            ContainerDelivery.Core.Exceptions.UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", context.Exception.Message, "UNAUTHORIZED", null),
            ContainerDelivery.Core.Exceptions.ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", context.Exception.Message, "FORBIDDEN", null),
            ContainerDelivery.Core.Exceptions.InvalidOperationException ioe => (StatusCodes.Status400BadRequest, "Bad Request", ioe.Message, ioe.ErrorCode, ioe.Details),
            ContainerDelivery.Core.Exceptions.MfaException mfe => (StatusCodes.Status400BadRequest, "MFA Error", mfe.Message, mfe.ErrorCode, null),
            ContainerDelivery.Core.Exceptions.ImportException ie => (StatusCodes.Status400BadRequest, "Import Error", ie.Message, ie.ErrorCode, ie.Details),
            ContainerDelivery.Core.Exceptions.ConcurrencyException ce => (StatusCodes.Status409Conflict, "Concurrency Error", ce.Message, ce.ErrorCode, ce.Details),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", _env.IsDevelopment() ? context.Exception.Message : "An unexpected error occurred", "INTERNAL_ERROR", null)
        };

        context.HttpContext.Response.StatusCode = statusCode;
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.HttpContext.Request.Path,
            Type = $"https://tools.ietf.org/html/rfc{statusCode}"
        };

        if (errorCode != null)
            problemDetails.Extensions["errorCode"] = errorCode;
        if (details != null)
            problemDetails.Extensions["details"] = details;
        if (_env.IsDevelopment() && context.Exception is not ContainerDelivery.Core.Exceptions.ContainerDeliveryException)
            problemDetails.Extensions["stackTrace"] = context.Exception.StackTrace;

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };

        context.ExceptionHandled = true;
    }
}