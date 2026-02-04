using System.Collections.Concurrent;
using System.Net;

namespace EveUp.Api.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // Configuration
    private const int MaxRequestsPerWindow = 100;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    // In-memory store (for single-instance MVP; use Redis for distributed)
    private static readonly ConcurrentDictionary<string, ClientRateInfo> _clients = new();

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientKey = GetClientKey(context);
        var now = DateTime.UtcNow;

        var clientInfo = _clients.AddOrUpdate(
            clientKey,
            _ => new ClientRateInfo { WindowStart = now, RequestCount = 1 },
            (_, existing) =>
            {
                if (now - existing.WindowStart > Window)
                {
                    // Reset window
                    existing.WindowStart = now;
                    existing.RequestCount = 1;
                }
                else
                {
                    existing.RequestCount++;
                }
                return existing;
            });

        var remaining = Math.Max(0, MaxRequestsPerWindow - clientInfo.RequestCount);
        var resetAt = clientInfo.WindowStart + Window;

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = MaxRequestsPerWindow.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)resetAt).ToUnixTimeSeconds().ToString();

        if (clientInfo.RequestCount > MaxRequestsPerWindow)
        {
            _logger.LogWarning(
                "[RateLimit] Client {ClientKey} exceeded rate limit: {Count}/{Max} requests in window",
                clientKey, clientInfo.RequestCount, MaxRequestsPerWindow);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = ((int)(resetAt - now).TotalSeconds).ToString();

            await context.Response.WriteAsync(
                "{\"error\":\"RATE_LIMIT_EXCEEDED\",\"message\":\"Too many requests. Please try again later.\",\"statusCode\":429}");
            return;
        }

        await _next(context);
    }

    private static string GetClientKey(HttpContext context)
    {
        // Use authenticated user ID if available, otherwise IP address
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private class ClientRateInfo
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}
