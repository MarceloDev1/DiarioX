using DiarioX.Server.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;

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
        var host = GetRequiredSetting(smtp, "Host");
        var port = int.TryParse(smtp["Port"], out var parsedPort) ? parsedPort : 587;
        var username = GetRequiredSetting(smtp, "Username");
        var password = GetRequiredSetting(smtp, "Password");
        var fromEmail = GetOptionalSetting(smtp, "FromEmail") ?? username;
        var fromName = smtp["FromName"] ?? "Diário de Classe";
        var recipientEmail = toEmail?.Trim();

        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException("O e-mail de destino não pode ser vazio.", nameof(toEmail));

        var message = new MimeMessage();
        message.MessageId = MimeUtils.GenerateMessageId();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName ?? recipientEmail, recipientEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("E-mail enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Email}", toEmail);
            throw;
        }
    }

    private static string GetRequiredSetting(IConfigurationSection section, string key)
    {
        var value = section[key]?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Configuração SMTP inválida: 'Smtp:{key}' está ausente ou vazia.");

        return value;
    }

    private static string? GetOptionalSetting(IConfigurationSection section, string key)
    {
        var value = section[key]?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
