using NewsAPI.Models;

namespace MyNewsApi.Infra.Clients;

public interface INewsApiClient
{
    ArticlesResult GetEverything(EverythingRequest request);
}