using System.Threading.Channels;
using CMS.Domain.Models;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Api.Middleware
{
    /// <summary>A finished request whose log row should be written (insert) or completed (update).</summary>
    public sealed record ActivityLogWork(
        ApiActivityLog? Insert,          // set => insert this full row (read-only requests)
        long UpdateId,                   // used when Insert is null
        string ResponseBody,
        int StatusCode,
        int ExecutionTimeMs);

    /// <summary>
    /// In-memory hand-off between the request pipeline and the DB writer, so logging
    /// never adds database latency to the response.
    /// </summary>
    public sealed class ActivityLogQueue
    {
        private readonly Channel<ActivityLogWork> _channel =
            Channel.CreateBounded<ActivityLogWork>(new BoundedChannelOptions(10_000)
            {
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropWrite // never block a request because the log is behind
            });

        public bool TryEnqueue(ActivityLogWork work) => _channel.Writer.TryWrite(work);
        public ChannelReader<ActivityLogWork> Reader => _channel.Reader;
        public void Complete() => _channel.Writer.TryComplete();
    }

    public sealed class ActivityLogWriter : BackgroundService
    {
        private const int MaxBatch = 100;
        private readonly ActivityLogQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ActivityLogWriter> _logger;

        public ActivityLogWriter(ActivityLogQueue queue, IServiceScopeFactory scopes, ILogger<ActivityLogWriter> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<ActivityLogWork>(MaxBatch);
            var reader = _queue.Reader;

            try
            {
                while (await reader.WaitToReadAsync(stoppingToken))
                {
                    batch.Clear();
                    while (batch.Count < MaxBatch && reader.TryRead(out var item))
                        batch.Add(item);

                    await FlushAsync(batch);
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }

            // Best-effort drain on shutdown.
            batch.Clear();
            while (reader.TryRead(out var item)) batch.Add(item);
            if (batch.Count > 0) await FlushAsync(batch);
        }

        private async Task FlushAsync(List<ActivityLogWork> batch)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var db = await factory.CreateDbContextAsync();
                db.Database.SetCommandTimeout(15);

                var inserts = batch.Where(b => b.Insert is not null).ToList();
                if (inserts.Count > 0)
                {
                    foreach (var i in inserts)
                    {
                        i.Insert!.ResponseBody = i.ResponseBody;
                        i.Insert.StatusCode = i.StatusCode;
                        i.Insert.ExecutionTimeMs = i.ExecutionTimeMs;
                    }
                    db.ApiActivityLog.AddRange(inserts.Select(i => i.Insert!));
                    await db.SaveChangesAsync();
                }

                foreach (var u in batch.Where(b => b.Insert is null))
                {
                    await db.ApiActivityLog
                        .Where(x => x.Id == u.UpdateId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.ResponseBody, u.ResponseBody)
                            .SetProperty(x => x.StatusCode, u.StatusCode)
                            .SetProperty(x => x.ExecutionTimeMs, u.ExecutionTimeMs));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write {Count} API activity log entries", batch.Count);
            }
        }
    }
}
