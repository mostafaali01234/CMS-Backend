using CMS_Backend.Helpers;

namespace CMS_Backend.Services;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddCorrelationIdMiddleware(this IApplicationBuilder builder) 
        => builder.UseMiddleware<CorrelationIdMiddleware>();
}
