namespace MyCondo.Domain.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
