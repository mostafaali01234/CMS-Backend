using CMS.Api.Helpers;

namespace CMS.Api.Services;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddCorrelationIdMiddleware(this IApplicationBuilder builder) 
        => builder.UseMiddleware<CorrelationIdMiddleware>();
}
