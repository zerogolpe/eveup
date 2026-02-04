namespace EveUp.Core.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string message);
    Task SendEmailAsync(string email, string subject, string body);
}
