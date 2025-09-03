namespace MyNewsApi.Application.DTOs;

public record PagedResult<T>(IEnumerable<T> Data, long Total, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}