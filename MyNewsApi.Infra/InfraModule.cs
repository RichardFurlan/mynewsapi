using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyNewsApi.Infra.Clients;
using MyNewsApi.Infra.Data;

namespace MyNewsApi.Infra;

public static class InfraModule
{
    public static IServiceCollection AddInfra(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddServices()
            .AddData(configuration);
        
        return services;
    }
    
    
    private static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("NewsDb")));

        return services;
    }
    
    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<INewsApiClient>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var key = cfg["NewsApi:ApiKey"];
            return new NewsApiClientWrapper(key ?? string.Empty);
        });
        return services;
    }

    
}