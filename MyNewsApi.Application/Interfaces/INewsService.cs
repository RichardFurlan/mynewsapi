using MyNewsApi.Application.DTOs;

namespace MyNewsApi.Application.Interfaces;

public interface INewsService
{
    Task<ResultViewModel<PagedResult<NewsDto>>> GetNewsPagedAsync(int page = 1, int pageSize = 20, int? userId = null,
        CancellationToken ct = default);

    Task<ResultViewModel<PagedResult<NewsDto>>> SearchSaveAndGetPagedAsync(
        string keyword,
        int userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
}