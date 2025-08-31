using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyNewsApi.Data;
using MyNewsApi.Models;
using NewsAPI;
using NewsAPI.Constants;
using NewsAPI.Models;

namespace MyNewsApi.Services;

public class NewsService
{
    private readonly NewsApiClient _client;
    private readonly AppDbContext _db;
    private readonly ILogger<NewsService> _logger;
    
    public NewsService(IConfiguration config, AppDbContext db, ILogger<NewsService> logger)
    {
        _db = db;
        _logger = logger;
        var apiKey = config["NewsApi:ApiKey"]; // pega do appsettings.json
        _client = new NewsApiClient(apiKey);
    }

    public async Task<List<News>> GetNewsAsync(string keyword, int userId, CancellationToken ct = default)
    {
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

        await _db.SaveChangesAsync(ct);
        return saved;
    }

    private string ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var stopwords = new HashSet<string>{
            "the","and","is","in","at","of","a","to","on","with","for","by","from","that","this","it","are","as","was","be"
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
}