namespace ContainerDelivery.Api.Models;

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