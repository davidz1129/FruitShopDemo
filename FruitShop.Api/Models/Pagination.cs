using System.ComponentModel.DataAnnotations;

namespace FruitShop.Api.Models;

public sealed record PageRequest
{
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;
    public const int MaximumPageNumber = int.MaxValue / MaximumPageSize + 1;

    [Range(1, MaximumPageNumber)]
    public int Page { get; init; } = 1;

    [Range(1, MaximumPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public int Skip => (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}