namespace ContainerDelivery.Core.Exceptions;

public class ContainerDeliveryException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    public ContainerDeliveryException(string message, string errorCode, object? details = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

public class EntityNotFoundException : ContainerDeliveryException
{
    public EntityNotFoundException(string entityName, int id) 
        : base($"{entityName} with ID {id} was not found", "ENTITY_NOT_FOUND", new { EntityName = entityName, Id = id })
    {
    }
}

public class DuplicateEntityException : ContainerDeliveryException
{
    public DuplicateEntityException(string entityName, string field, string value) 
        : base($"{entityName} with {field} '{value}' already exists", "DUPLICATE_ENTITY", new { EntityName = entityName, Field = field, Value = value })
    {
    }
}

public class InvalidOperationException : ContainerDeliveryException
{
    public InvalidOperationException(string message, string errorCode, object? details = null) 
        : base(message, errorCode, details)
    {
    }
}

public class UnauthorizedException : ContainerDeliveryException
{
    public UnauthorizedException(string message = "Unauthorized access") 
        : base(message, "UNAUTHORIZED")
    {
    }
}

public class ForbiddenException : ContainerDeliveryException
{
    public ForbiddenException(string message = "Access denied") 
        : base(message, "FORBIDDEN")
    {
    }
}

public class ValidationException : ContainerDeliveryException
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors) 
        : base("Validation failed", "VALIDATION_ERROR", errors)
    {
        Errors = errors;
    }
}

public class ConcurrencyException : ContainerDeliveryException
{
    public ConcurrencyException(string entityName, int id) 
        : base($"{entityName} with ID {id} was modified by another user", "CONCURRENCY_ERROR", new { EntityName = entityName, Id = id })
    {
    }
}

public class ImportException : ContainerDeliveryException
{
    public ImportException(string message, object? details = null) 
        : base(message, "IMPORT_ERROR", details)
    {
    }
}

public class MfaException : ContainerDeliveryException
{
    public MfaException(string message) 
        : base(message, "MFA_ERROR")
    {
    }
}