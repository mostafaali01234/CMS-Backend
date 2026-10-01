using CMS.Api.Middleware;

namespace CMS.Application.Services;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddCorrelationIdMiddleware(this IApplicationBuilder builder) 
        => builder.UseMiddleware<CorrelationIdMiddleware>();
}
