using System.Diagnostics;
using System.Security.Claims;

namespace EveUp.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..12];

        // Extract user info if authenticated
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var method = context.Request.Method;
        var path = context.Request.Path;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation(
            "[{RequestId}] {Method} {Path} | User: {UserId} | IP: {Ip}",
            requestId, method, path, userId, ip);

        // Add request ID to response headers for tracing
        context.Response.Headers["X-Request-Id"] = requestId;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            if (statusCode >= 500)
            {
                _logger.LogError(
                    "[{RequestId}] {Method} {Path} → {StatusCode} ({ElapsedMs}ms) | User: {UserId}",
                    requestId, method, path, statusCode, stopwatch.ElapsedMilliseconds, userId);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "[{RequestId}] {Method} {Path} → {StatusCode} ({ElapsedMs}ms) | User: {UserId}",
                    requestId, method, path, statusCode, stopwatch.ElapsedMilliseconds, userId);
            }
            else
            {
                _logger.LogInformation(
                    "[{RequestId}] {Method} {Path} → {StatusCode} ({ElapsedMs}ms)",
                    requestId, method, path, statusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
