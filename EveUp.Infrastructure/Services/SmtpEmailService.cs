using EveUp.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EveUp.Infrastructure.Services;

public class SmtpEmailService : INotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task SendAsync(Guid userId, string title, string message)
    {
        _logger.LogInformation("[Notification] Push para userId={UserId}: {Title}", userId, title);
        return Task.CompletedTask;
    }

    public async Task SendEmailAsync(string email, string subject, string body)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var smtpUser = _config["Email:SmtpUser"] ?? "";
        var smtpPassword = _config["Email:SmtpPassword"] ?? "";
        var fromEmail = _config["Email:FromEmail"] ?? smtpUser;
        var fromName = _config["Email:FromName"] ?? "EveUp";

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogWarning("[Email] SMTP not configured. Email NOT sent to {Email}: {Subject}", email, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress("", email));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("[Email] Sent to {Email}: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send to {Email}: {Subject}", email, subject);
        }
    }
}
