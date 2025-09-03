using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyNewsApi.Application.DTOs;
using MyNewsApi.Application.Interfaces;
using MyNewsApi.Domain.Entities;
using MyNewsApi.Infra.Clients;
using MyNewsApi.Infra.Data;
using NewsAPI;
using NewsAPI.Constants;
using NewsAPI.Models;

namespace MyNewsApi.Application.Services;

public class NewsService : INewsService
{
    private readonly INewsApiClient _client;
    private readonly AppDbContext _db;
    private readonly ILogger<NewsService> _logger;

    public NewsService(IConfiguration config, AppDbContext db, ILogger<NewsService> logger, INewsApiClient? client = null)
    {
        _db = db;
        _logger = logger;
        if (client != null)
        {
            _client = client;
        }
        else
        {
            var apiKey = config["NewsApi:ApiKey"];
            if (apiKey != null) _client = new NewsApiClientWrapper(apiKey);
        }
    }


    #region GetNewsPagedAsync
    public async Task<ResultViewModel<PagedResult<NewsDto>>> GetNewsPagedAsync(int page = 1, int pageSize = 20, int? userId = null,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        
        var query = _db.News.AsNoTracking().AsQueryable();

        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId);

        var total = await query.LongCountAsync();
        
        var items = await query 
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select( n => new NewsDto (
                n.Id,
                n.Title,
                n.Description,
                n.Content,
                n.Url,
                n.UrlToImage,
                n.PublishedAt,
                n.SourceName, 
                n.UserId
            ))
            .ToListAsync(ct);

        var paged = new PagedResult<NewsDto>(items, total, page, pageSize);
        return ResultViewModel<PagedResult<NewsDto>>.Success(paged);
    }
    

    #endregion

    #region SearchSaveAndGetPagedAsync
    public async Task<ResultViewModel<PagedResult<NewsDto>>> SearchSaveAndGetPagedAsync(
        string keyword,
        int userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {

        if (string.IsNullOrWhiteSpace(keyword))
            return ResultViewModel<PagedResult<NewsDto>>.Error("A palavra-chave é obrigatória.");
        
        await FetchAndSaveNewsAsync(keyword, userId, ct);
        
        var query = _db.News.AsNoTracking()
            .Where(n => n.UserId == userId &&
                        (n.Title.Contains(keyword) || 
                         n.Description.Contains(keyword) || 
                         n.Content.Contains(keyword)));
        
        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NewsDto(
                n.Id,
                n.Title,
                n.Description,
                n.Content,
                n.Url,
                n.UrlToImage,
                n.PublishedAt,
                n.SourceName,
                n.UserId
            ))
            .ToListAsync(ct);

        if (!items.Any())
            return ResultViewModel<PagedResult<NewsDto>>.Error("Nenhuma notícia encontrada");
        
        var paged = new PagedResult<NewsDto>(items, total, page, pageSize);
        return ResultViewModel<PagedResult<NewsDto>>.Success(paged);
    }
    
    #endregion

    #region FetchAndSaveNewsAsync
    private async Task<List<News>> FetchAndSaveNewsAsync(string keyword, int userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<News>();
        
        _logger.LogInformation("Fetching news for '{Keyword}' (userId={UserId}", keyword, userId);
        
        var response = await Task.Run(() =>
                _client.GetEverything(new EverythingRequest
                {
                    Q = keyword,
                    SortBy = SortBys.Popularity,
                    Language = Languages.EN,
                    PageSize = 100,
                    From = DateTime.UtcNow.AddDays(-7) // últimas notícias da semana
                }), ct
        );

        if (response == null || response.Status != Statuses.Ok || response.Articles == null)
        {
            _logger.LogWarning("NewsAPI retornou vazio/erro para keyword={Keyword}", keyword);
            return new List<News>();
        }

        var saved = new List<News>();

        foreach (var article in response.Articles)
        {
            if (string.IsNullOrWhiteSpace(article.Url)) continue;

            var exists = await _db.News.AnyAsync(x => x.Url == article.Url, ct);
            if (exists) continue;

            var entity = new News
            (
                article.Title ?? string.Empty,
                article.Author ?? string.Empty,
                article.Description ?? string.Empty,
                article.Url ?? string.Empty,
                article.UrlToImage ?? string.Empty,
                article.Content ?? string.Empty,
                article.PublishedAt ?? DateTime.UtcNow,
                article.Source?.Name ?? string.Empty,
                article.Source?.Id ?? string.Empty,
                nameof(Languages.EN),
                userId
            );

            entity.SetKeywords(ExtractKeywords($"{entity.Title} {entity.Description} {entity.Content}"));

            _db.News.Add(entity);
            saved.Add(entity);

        }
        
        if (saved.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Saved {Count} new articles for user {UserId}", saved.Count, userId);
        }
        else
        {
            _logger.LogInformation("No new articles to save for user {UserId}", userId);
        }
        
        await _db.SaveChangesAsync(ct);

        return saved;
    }

    #endregion

    #region ExtractKeywords

    private string ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var stopwords = new HashSet<string>
        {
            "the", "and", "is", "in", "at", "of", "a", "to", "on", "with", "for", "by", "from", "that", "this", "it",
            "are", "as", "was", "be"
        };

        var tokens = Regex.Matches(text.ToLowerInvariant(), @"\b[a-z]{3,}\b")
            .Select(m => m.Value)
            .Where(w => !stopwords.Contains(w))
            .GroupBy(w => w)
            .Select(g => new { Word = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(7)
            .Select(x => x.Word);

        return string.Join(",", tokens);
    }

    #endregion
    
}