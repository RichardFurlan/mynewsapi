using Microsoft.Extensions.DependencyInjection;
using MyNewsApi.Application.Interfaces;
using MyNewsApi.Application.Services;

namespace MyNewsApi.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddAplication(this IServiceCollection services)
    {
        services.AddScoped();
        return services;
    }
    
    private static IServiceCollection AddScoped(this IServiceCollection services)
    {
        services.AddScoped<NewsService>();
        services.AddScoped<AuthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INewsService, NewsService>();
        
        return services;
    }

}