using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Application.Common;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public static PagedResponse<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PagedResponse<T>(items, totalCount, page, pageSize, totalPages);
    }
}

public record ApiResponse<T>(
    bool Success,
    T? Data = default,
    string? Message = null,
    IEnumerable<string>? Errors = null
)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, data, message);
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new(false, default, message, errors);
}

public record ApiResponse(
    bool Success,
    string? Message = null,
    IEnumerable<string>? Errors = null
)
{
    public static ApiResponse Ok(string? message = null) => new(true, message);
    public static ApiResponse Fail(string message, IEnumerable<string>? errors = null) => new(false, message, errors);
}

public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public void Normalize()
    {
        Page = Math.Max(1, Page);
        PageSize = Math.Clamp(PageSize, 1, 100);
    }
}