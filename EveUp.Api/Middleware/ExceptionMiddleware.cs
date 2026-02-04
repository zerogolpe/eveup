using System.Net;
using System.Text.Json;
using EveUp.Core.Exceptions;

namespace EveUp.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errorCode) = exception switch
        {
            InvalidStateTransitionException ex => (
                HttpStatusCode.Conflict,
                ex.Message,
                "INVALID_STATE_TRANSITION"
            ),
            BusinessRuleException ex => (
                HttpStatusCode.BadRequest,
                ex.Message,
                ex.Rule
            ),
            PaymentFailedException ex => (
                HttpStatusCode.PaymentRequired,
                ex.Message,
                ex.FailureCode ?? "PAYMENT_FAILED"
            ),
            Core.Exceptions.AuthenticationException => (
                HttpStatusCode.Unauthorized,
                "Authentication failed.",
                "AUTHENTICATION_FAILED"
            ),
            TokenExpiredException => (
                HttpStatusCode.Unauthorized,
                "Token expired.",
                "TOKEN_EXPIRED"
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Access denied.",
                "UNAUTHORIZED"
            ),
            ArgumentException ex => (
                HttpStatusCode.BadRequest,
                ex.Message,
                "INVALID_ARGUMENT"
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                "INTERNAL_ERROR"
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Business exception [{ErrorCode}]: {Message}", errorCode, message);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new
        {
            error = errorCode,
            message,
            statusCode = (int)statusCode
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(response);
    }
}
