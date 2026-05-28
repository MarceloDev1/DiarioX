using System.Net;
using System.Net.Mail;
using DiarioX.Server.Application.Interfaces;

namespace DiarioX.Server.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string? toName, string subject, string htmlBody)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"]!;
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"]!;
        var password = smtp["Password"]!;
        var fromEmail = smtp["FromEmail"] ?? username;
        var fromName = smtp["FromName"] ?? "Diário de Classe";

        using var client = new SmtpClient(host, port)
        {
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };

        message.To.Add(toName is not null ? new MailAddress(toEmail, toName) : new MailAddress(toEmail));

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("E-mail enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Email}", toEmail);
            throw;
        }
    }
}
