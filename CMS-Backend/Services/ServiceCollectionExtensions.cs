using CMS.Api.Configuration;
using CMS.Api.Configuration.Interfaces;

namespace CMS.Api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorrelationIdManager(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();

        return services;
    }
}
