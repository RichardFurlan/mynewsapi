using NewsAPI;
using NewsAPI.Models;

namespace MyNewsApi.Infra.Clients;

public class NewsApiClientWrapper : INewsApiClient
{
    private readonly NewsApiClient _client;

    public NewsApiClientWrapper(string apiKey)
    {
        _client = new NewsApiClient(apiKey);
    }

    public ArticlesResult GetEverything(EverythingRequest request)
    {
        return _client.GetEverything(request);
    }
}