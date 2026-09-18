using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DiarioX.Server.Application.Interfaces;

namespace DiarioX.Server.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task SendAsync(string toEmail, string? toName, string subject, string htmlBody)
    {
        var smtp = _configuration.GetSection("Smtp");
        var apiKey = smtp["ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Configuração SMTP inválida: 'Smtp:ApiKey' está ausente ou vazia.");

        var fromEmail = smtp["FromEmail"] ?? "noreply@diariox.online";
        var fromName = smtp["FromName"] ?? "Diário de Classe";
        var recipientEmail = toEmail?.Trim();

        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException("O e-mail de destino não pode ser vazio.", nameof(toEmail));

        var payload = new
        {
            sender = new { name = fromName, email = fromEmail },
            to = new[] { new { email = recipientEmail, name = toName ?? recipientEmail } },
            subject,
            htmlContent = htmlBody
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo API erro {Status}: {Body}", (int)response.StatusCode, responseBody);
                throw new InvalidOperationException($"Falha ao enviar e-mail via Brevo: {response.StatusCode}");
            }

            _logger.LogInformation("E-mail enviado para {Email}", toEmail);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Email}", toEmail);
            throw;
        }
    }
}
