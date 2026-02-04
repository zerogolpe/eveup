using EveUp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EveUp.Infrastructure.Services;

/// <summary>
/// Mock Notification Service for MVP. Logs notifications instead of sending them.
/// Replace with real email/push notification provider in production.
/// </summary>
public class MockNotificationService : INotificationService
{
    private readonly ILogger<MockNotificationService> _logger;

    public MockNotificationService(ILogger<MockNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Guid userId, string title, string message)
    {
        _logger.LogInformation(
            "[MockNotification] Push → userId={UserId}, title={Title}, message={Message}",
            userId, title, message);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string email, string subject, string body)
    {
        _logger.LogInformation(
            "[MockNotification] Email → to={Email}, subject={Subject}, body={Body}",
            email, subject, body);
        return Task.CompletedTask;
    }
}
