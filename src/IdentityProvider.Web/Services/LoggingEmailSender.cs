using System.Net;
using System.Net.Mail;

namespace IdentityProvider.Web.Services;

/// <summary>
/// Logs every message. Sends via SMTP when Smtp:Host is configured.
/// Swap this for Azure Communication Services or SendGrid later.
/// </summary>
public sealed class LoggingEmailSender : IAppEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly IWebHostEnvironment _environment;

    public LoggingEmailSender(
        IConfiguration configuration,
        ILogger<LoggingEmailSender> logger,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email queued. To={To} Subject={Subject} Body={Body}",
            to,
            subject,
            htmlBody);

        if (_environment.IsDevelopment())
        {
            var mailbox = Path.Combine(_environment.ContentRootPath, "logs");
            Directory.CreateDirectory(mailbox);
            var file = Path.Combine(mailbox, "emails.log");
            await File.AppendAllTextAsync(
                file,
                $"[{DateTimeOffset.UtcNow:O}] To={to} Subject={subject}{Environment.NewLine}{htmlBody}{Environment.NewLine}{Environment.NewLine}",
                cancellationToken);
        }

        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_configuration["Smtp:From"] ?? "noreply@localhost"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(host, _configuration.GetValue("Smtp:Port", 587))
        {
            EnableSsl = true
        };

        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];
        if (!string.IsNullOrEmpty(user))
        {
            client.Credentials = new NetworkCredential(user, password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
