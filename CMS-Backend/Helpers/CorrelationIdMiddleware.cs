using CMS_Backend.Configuration.Interfaces;

namespace CMS_Backend.Helpers;
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string _correlationHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
    {
        var correlationId = GetCorrelationIdTrace(context, correlationIdGenerator);
        AddCorrelationIdToResponse(context, correlationId);

        await _next(context);
    }

    private static string GetCorrelationIdTrace(HttpContext context, ICorrelationIdGenerator correlationIdGenerator)
    {
        if(context.Request.Headers.TryGetValue(_correlationHeader, out var correlationId))
        {
            correlationIdGenerator.Set(correlationId);
            return correlationId;
        }
        else
        {
            return correlationIdGenerator.Get();
        }
    }

    private static void AddCorrelationIdToResponse(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Add(_correlationHeader, new[] { correlationId });
            return Task.CompletedTask;
        });
    }
}
