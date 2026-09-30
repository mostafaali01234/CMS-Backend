using CMS.Application.Interfaces.Configuration;
using CMS_Backend.Services.Configuration;

namespace CMS.Api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorrelationIdManager(this IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();

        return services;
    }
}
