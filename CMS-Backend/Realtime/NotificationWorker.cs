using CMS.Application.Interfaces;

namespace CMS.Api.Realtime;

/// <summary>
/// Runs for the whole life of the app. It takes jobs off the queue one by one and, for each:
///   1. saves the notification rows (one per recipient)
///   2. pushes each one live to the recipient's open browser tabs
/// Doing this here, not inside the request, is what keeps "change order status" fast.
/// </summary>
public class NotificationWorker : BackgroundService
{
    // The request that changed the order may not have committed yet when we pick the job up (or it may roll back).
    // So the service checks the order really has the new status; if not, we wait a moment and look again.
    private const int MaxAttempts = 6;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly INotificationQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(INotificationQueue queue, IServiceScopeFactory scopes, ILogger<NotificationWorker> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                // each job in its own task so one waiting for a commit never holds up the others
                _ = Task.Run(() => HandleAsync(job, stoppingToken), stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
    }

    private async Task HandleAsync(NotificationRequest job, CancellationToken ct)
    {
        try
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                // a fresh scope = a fresh DbContext, because we are outside any web request
                using var scope = _scopes.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var pusher = scope.ServiceProvider.GetRequiredService<IRealtimePusher>();

                var created = await service.CreateAsync(job);
                if (created is null)                       // not committed yet
                {
                    await Task.Delay(RetryDelay, ct);
                    continue;
                }

                foreach (var (userId, dto) in created)
                {
                    try { await pusher.PushNotificationAsync(userId, dto); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Live push to {UserId} failed (saved; they will see it on refresh)", userId); }
                }
                return;
            }

            _logger.LogInformation("Dropped notification for order {OrderId}: status never became {Status}",
                job.OrderChange?.OrderId, job.OrderChange?.New);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // a notification problem must never take the app down
            _logger.LogError(ex, "Could not process notification job {Type}", job.Type);
        }
    }
}
