using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
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

            // Provide structured JSON (405 Method Not Allowed or 404 Not Found) when an /api/* route
            // does not reach an action (e.g. removed endpoint, or GET/POST on a PUT/DELETE-only endpoint).
            if (!context.Response.HasStarted &&
                ((context.Response.StatusCode == StatusCodes.Status404NotFound && context.GetEndpoint() == null) ||
                 (context.Response.StatusCode == StatusCodes.Status405MethodNotAllowed && string.IsNullOrEmpty(context.Response.ContentType))))
            {
                var allowedMethods = FindMatchingApiMethods(context);
                if (allowedMethods.Count > 0 &&
                    !allowedMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    context.Response.Headers["Allow"] = string.Join(", ", allowedMethods);
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var payload = JsonSerializer.Serialize(new
                    {
                        success = false,
                        status = 405,
                        method = context.Request.Method,
                        path = context.Request.Path.Value,
                        allowedMethods,
                        message = $"HTTP {context.Request.Method} is not supported for {context.Request.Path}. Use {string.Join("/", allowedMethods)} (or trigger this action in the app UI and inspect Fetch/XHR in DevTools Network -> Copy as cURL)."
                    });
                    await context.Response.WriteAsync(payload);
                    return;
                }

                context.Response.Headers.Remove("Allow");
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json; charset=utf-8";
                var notFoundPayload = JsonSerializer.Serialize(new
                {
                    success = false,
                    status = 404,
                    method = context.Request.Method,
                    path = context.Request.Path.Value,
                    message = $"API endpoint '{context.Request.Path}' was not found."
                });
                await context.Response.WriteAsync(notFoundPayload);
            }
        }

        private static List<string> FindMatchingApiMethods(HttpContext context)
        {
            var dataSources = context.RequestServices.GetServices<EndpointDataSource>();
            var reqSegments = (context.Request.Path.Value ?? string.Empty)
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            var methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ds in dataSources)
            {
                foreach (var endpoint in ds.Endpoints.OfType<RouteEndpoint>())
                {
                    var raw = endpoint.RoutePattern.RawText;
                    if (string.IsNullOrWhiteSpace(raw) ||
                        !raw.TrimStart('/').StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var routeSegments = raw.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (!RouteSegmentsMatch(routeSegments, reqSegments))
                    {
                        continue;
                    }

                    var httpMeta = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
                    if (httpMeta != null)
                    {
                        foreach (var m in httpMeta.HttpMethods)
                        {
                            methods.Add(m);
                        }
                    }
                }
            }

            return methods.ToList();
        }

        private static bool RouteSegmentsMatch(string[] routeSegments, string[] reqSegments)
        {
            if (routeSegments.Length != reqSegments.Length)
            {
                return false;
            }

            for (int i = 0; i < routeSegments.Length; i++)
            {
                var rSeg = routeSegments[i];
                var qSeg = reqSegments[i];

                if (rSeg.StartsWith("{", StringComparison.Ordinal) && rSeg.EndsWith("}", StringComparison.Ordinal))
                {
                    var inner = rSeg.Substring(1, rSeg.Length - 2);
                    if (inner.EndsWith(":int", StringComparison.OrdinalIgnoreCase) && !int.TryParse(qSeg, out _))
                    {
                        return false;
                    }
                    if (inner.EndsWith(":long", StringComparison.OrdinalIgnoreCase) && !long.TryParse(qSeg, out _))
                    {
                        return false;
                    }
                    continue;
                }

                if (!string.Equals(rSeg, qSeg, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
