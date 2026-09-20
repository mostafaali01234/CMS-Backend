using CMS_Backend.Data;
using CMS_Backend.Helpers;
using CMS_Backend.Models;
using System.Diagnostics;

namespace CMS_Backend.Middleware
{
    public class ApiActivityLogMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiActivityLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, AppDbContext db)
        {
            var watch = Stopwatch.StartNew();

            // Read request body
            context.Request.EnableBuffering();
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Capture response
            var originalBody = context.Response.Body;
            using var newBody = new MemoryStream();
            context.Response.Body = newBody;

            await _next(context);

            watch.Stop();

            // Read response
            newBody.Position = 0;
            var responseBody = await new StreamReader(newBody).ReadToEndAsync();
            newBody.Position = 0;

            await newBody.CopyToAsync(originalBody);

            // Save log entry
            var log = new ApiActivityLog
            {
                UserId = (context.User.FindFirst("UserId")?.Value) ?? "",
                Endpoint = context.Request.Path,
                HttpMethod = context.Request.Method,
                RequestBody = SensitiveDataRedactor.Mask(requestBody),
                ResponseBody = SensitiveDataRedactor.Mask(responseBody),
                StatusCode = context.Response.StatusCode,
                ExecutionTimeMs = (int)watch.ElapsedMilliseconds,
                IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = context.Request?.Headers["User-Agent"] ?? "",
                CreatedDate = DateTime.Now
            };

            db.ApiActivityLog.Add(log);
            await db.SaveChangesAsync();
            context.Items["CurrentLogId"] = log.Id;
        }
    }
}
