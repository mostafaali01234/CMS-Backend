using CMS.Api.Data;
using CMS.Api.Helpers;
using CMS.Domain.Models;
using System.Diagnostics;

namespace CMS.Api.Middleware
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
            var requestBody = await new StreamReader(context.Request.Body, leaveOpen: true).ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Create the log row FIRST — before _next(context) — so that
            // CurrentLogId is already in context.Items for the ENTIRE request,
            // including whatever SaveChangesAsync calls happen inside controllers/services.
            var log = new ApiActivityLog
            {
                UserId = context.User?.FindFirst("UserId")?.Value ?? "",
                Endpoint = context.Request.Path,
                HttpMethod = context.Request.Method,
                RequestBody = SensitiveDataRedactor.Mask(requestBody),
                ResponseBody = "",
                IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                CreatedDate = DateTime.Now
            };

            db.ApiActivityLog.Add(log);
            await db.SaveChangesAsync();               // Id is generated here
            context.Items["CurrentLogId"] = log.Id;     // now available to the interceptor during _next

            // Capture response
            var originalBody = context.Response.Body;
            using var newBody = new MemoryStream();
            context.Response.Body = newBody;

            Exception? caught = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            watch.Stop();

            // Read response
            newBody.Position = 0;
            var responseBody = await new StreamReader(newBody).ReadToEndAsync();
            newBody.Position = 0;
            await newBody.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            // Update the SAME row with response info
            log.ResponseBody = SensitiveDataRedactor.Mask(responseBody);
            log.StatusCode = context.Response.StatusCode;
            log.ExecutionTimeMs = (int)watch.ElapsedMilliseconds;
            await db.SaveChangesAsync();

            if (caught != null)
                throw caught;
        }
    }
}
