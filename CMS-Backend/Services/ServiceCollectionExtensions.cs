using CMS_Backend.Configuration;
using CMS_Backend.Configuration.Interfaces;

namespace CMS_Backend.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorrelationIdManager(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();

        return services;
    }
}
