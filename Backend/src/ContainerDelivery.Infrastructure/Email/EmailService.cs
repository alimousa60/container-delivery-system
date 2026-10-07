using ContainerDelivery.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ContainerDelivery.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
            if (!string.IsNullOrEmpty(_settings.SmtpUser))
            {
                client.Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword);
                client.EnableSsl = true;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public Task SendPasswordResetAsync(string to, string resetLink)
    {
        var body = $"""
            <h2>Password Reset</h2>
            <p>Click the link below to reset your password:</p>
            <p><a href="{resetLink}">Reset Password</a></p>
            <p>This link expires in 30 minutes. If you didn't request this, ignore this email.</p>
            """;
        return SendAsync(to, "Reset Your Password", body);
    }

    public Task SendMfaSetupAsync(string to, string secret, string qrCodeUrl, string[] recoveryCodes)
    {
        var codes = string.Join(", ", recoveryCodes);
        var body = $"""
            <h2>MFA Setup</h2>
            <p>Your MFA secret: <code>{secret}</code></p>
            <p>QR Code URL: <a href="{qrCodeUrl}">{qrCodeUrl}</a></p>
            <p>Recovery codes (store them safely): <code>{codes}</code></p>
            """;
        return SendAsync(to, "MFA Setup - Container Delivery System", body);
    }
}
