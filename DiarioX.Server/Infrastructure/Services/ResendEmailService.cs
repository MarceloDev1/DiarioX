using DiarioX.Server.Application.Interfaces;
using Resend;

namespace DiarioX.Server.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IResend resend, IConfiguration configuration, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string? toName, string subject, string htmlBody)
    {
        var recipientEmail = toEmail?.Trim();
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException("O e-mail de destino não pode ser vazio.", nameof(toEmail));

        var resendSection = _configuration.GetSection("Resend");
        var fromEmail = resendSection["FromEmail"]?.Trim();
        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new InvalidOperationException("Configuração inválida: 'Resend:FromEmail' está ausente ou vazia.");

        var fromName = resendSection["FromName"]?.Trim();
        var from = string.IsNullOrWhiteSpace(fromName) ? fromEmail : $"{fromName} <{fromEmail}>";
        var to = string.IsNullOrWhiteSpace(toName) ? recipientEmail : $"{toName} <{recipientEmail}>";

        var message = new EmailMessage
        {
            From = from,
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
        };

        try
        {
            var resp = await _resend.EmailSendAsync(message);
            _logger.LogInformation("E-mail enviado para {Email} via Resend (Id: {Id})", recipientEmail, resp.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail via Resend para {Email}", recipientEmail);
            throw;
        }
    }
}
