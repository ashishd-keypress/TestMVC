using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace Utility
{
    /// <summary>
    /// Ensures all /api/* HTTP responses carry accurate execution timing,
    /// request metadata headers, and no-cache directives so browser DevTools Network tab
    /// captures full headers, payloads, responses, timings, and cURL exports consistently.
    /// </summary>
    public class ApiObservabilityMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiObservabilityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();
                double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
                string formattedMs = elapsedMs.ToString("F2", CultureInfo.InvariantCulture);

                var headers = context.Response.Headers;
                headers["X-Execution-Time-Ms"] = formattedMs;
                headers["Server-Timing"] = $"api;dur={formattedMs};desc=\"API Execution\"";
                headers["X-Request-Method"] = context.Request.Method;
                headers["X-Request-Url"] = $"{context.Request.Path}{context.Request.QueryString}";
                headers["X-Request-Id"] = context.TraceIdentifier;
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
                headers["Expires"] = "0";

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
