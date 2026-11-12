using System.Diagnostics;
using System.Text;
using CMS.Domain.Models;
using CMS.Infrastructure.Helpers;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Api.Middleware
{
    /// <summary>
    /// Logs every API call. Design goals:
    ///  - never add DB latency to the response (writes go through <see cref="ActivityLogQueue"/>),
    ///  - never buffer the whole response in memory (a pass-through tee keeps only the first bytes),
    ///  - never parse/re-serialize bodies (masking is a bounded regex over the truncated text),
    ///  - never fail the request because logging failed.
    /// </summary>
    public class ApiActivityLogMiddleware
    {
        private const int MaxBodyChars = 4000;
        private const int MaxRequestBufferBytes = 1024 * 1024;   // don't buffer/log bigger request bodies
        private const int MaxCaptureBytes = 16 * 1024;           // enough for 4000 chars of UTF-8 (incl. Arabic)

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiActivityLogMiddleware> _logger;
        private readonly ActivityLogQueue _queue;

        public ApiActivityLogMiddleware(RequestDelegate next, ILogger<ApiActivityLogMiddleware> logger, ActivityLogQueue queue)
        {
            _next = next;
            _logger = logger;
            _queue = queue;
        }

        public async Task Invoke(HttpContext context, IDbContextFactory<AppDbContext> factory)
        {
            if (ShouldSkip(context))
            {
                await _next(context);
                return;
            }

            var watch = Stopwatch.StartNew();
            var requestBody = await ReadRequestBodyAsync(context.Request);

            var log = new ApiActivityLog
            {
                UserId = context.User?.FindFirst("UserId")?.Value ?? "",
                Endpoint = Cut(context.Request.Path.Value, 500),
                HttpMethod = Cut(context.Request.Method, 50),
                RequestBody = SensitiveDataRedactor.MaskForLog(requestBody, MaxBodyChars),
                ResponseBody = "",
                IPAddress = Cut(context.Connection.RemoteIpAddress?.ToString(), 100),
                UserAgent = Cut(context.Request.Headers.UserAgent.ToString(), 500),
                CreatedDate = DateTime.Now
            };

            // Requests that can change data need the log id *during* the request
            // (the audit interceptor stamps it on every ApiDataChangeLog row), so
            // those are inserted up-front. Read-only requests are written once, afterwards.
            long? logId = null;
            var readOnly = HttpMethods.IsGet(log.HttpMethod) || HttpMethods.IsHead(log.HttpMethod);
            if (!readOnly)
            {
                try
                {
                    await using var db = await factory.CreateDbContextAsync(context.RequestAborted);
                    db.Database.SetCommandTimeout(5);
                    db.ApiActivityLog.Add(log);
                    await db.SaveChangesAsync(context.RequestAborted);
                    logId = log.Id;
                    context.Items["CurrentLogId"] = log.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not create API activity log row");
                }
            }

            var originalBody = context.Response.Body;
            var tee = new CaptureStream(originalBody, MaxCaptureBytes);
            context.Response.Body = tee;

            var failed = false;
            try
            {
                await _next(context);
            }
            catch
            {
                failed = true;
                throw;
            }
            finally
            {
                context.Response.Body = originalBody;   // always restore
                watch.Stop();

                try
                {
                    var status = failed && context.Response.StatusCode < 400
                        ? StatusCodes.Status500InternalServerError
                        : context.Response.StatusCode;

                    var response = IsTextual(context.Response.ContentType)
                        ? SensitiveDataRedactor.MaskForLog(tee.GetCapturedText(), MaxBodyChars)
                        : "";

                    var ms = (int)watch.ElapsedMilliseconds;

                    if (logId is long id)
                        _queue.TryEnqueue(new ActivityLogWork(null, id, response, status, ms));
                    else if (readOnly)
                        _queue.TryEnqueue(new ActivityLogWork(log, 0, response, status, ms));
                    // else: the up-front insert failed, nothing to update
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not queue API activity log");
                }
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            try
            {
                if (request.ContentLength is 0) return "";
                if (!IsTextual(request.ContentType)) return request.ContentLength is null or 0 ? "" : "[non-text body omitted]";
                if (request.ContentLength is long len && len > MaxRequestBufferBytes) return "[large body omitted]";

                request.EnableBuffering(MaxRequestBufferBytes);

                // Read only what we will store; the rest stays in the stream for model binding.
                using var reader = new StreamReader(request.Body, Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                var buffer = new char[MaxBodyChars];
                var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                request.Body.Position = 0;
                return new string(buffer, 0, read);
            }
            catch
            {
                if (request.Body.CanSeek) request.Body.Position = 0;
                return "";
            }
        }

        private static bool ShouldSkip(HttpContext c) =>
            HttpMethods.IsOptions(c.Request.Method) ||
            c.Request.Path.StartsWithSegments("/swagger") ||
            c.Request.Path.StartsWithSegments("/openapi") ||
            c.Request.Path.StartsWithSegments("/health")  ||
            c.Request.Path.StartsWithSegments("/hubs");   // SignalR: long-lived WebSocket, must not be wrapped

        private static bool IsTextual(string? contentType) =>
            !string.IsNullOrEmpty(contentType) &&
            (contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
             contentType.Contains("text/", StringComparison.OrdinalIgnoreCase) ||
             contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
             contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase));

        private static string Cut(string? s, int max) =>
            string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max];

        /// <summary>Write-through stream that remembers only the first <c>limit</c> bytes.</summary>
        private sealed class CaptureStream : Stream
        {
            private readonly Stream _inner;
            private readonly int _limit;
            private byte[]? _buf;
            private int _count;

            public CaptureStream(Stream inner, int limit) { _inner = inner; _limit = limit; }

            public string GetCapturedText()
            {
                if (_buf is null || _count == 0) return "";
                var s = Encoding.UTF8.GetString(_buf, 0, _count);
                // a multi-byte char cut at the capture limit decodes to U+FFFD; drop it
                return s.Length > 0 && s[^1] == '�' ? s[..^1] : s;
            }

            private void Capture(ReadOnlySpan<byte> data)
            {
                if (_count >= _limit || data.IsEmpty) return;
                _buf ??= new byte[_limit];
                var n = Math.Min(data.Length, _limit - _count);
                data[..n].CopyTo(_buf.AsSpan(_count));
                _count += n;
            }

            public override void Write(byte[] buffer, int offset, int count) { Capture(buffer.AsSpan(offset, count)); _inner.Write(buffer, offset, count); }
            public override void Write(ReadOnlySpan<byte> buffer) { Capture(buffer); _inner.Write(buffer); }
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) { Capture(buffer.AsSpan(offset, count)); return _inner.WriteAsync(buffer, offset, count, ct); }
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) { Capture(buffer.Span); return _inner.WriteAsync(buffer, ct); }
            public override void Flush() => _inner.Flush();
            public override Task FlushAsync(CancellationToken ct) => _inner.FlushAsync(ct);

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }
    }
}
