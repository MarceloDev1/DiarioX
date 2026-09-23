using DiarioX.Server.Application.Interfaces;

namespace DiarioX.Server.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de notificações por email.
/// Fornece templates formatados em HTML para diferentes tipos de notificação.
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly IConfiguration _configuration;

    public EmailNotificationService(IEmailService emailService, ILogger<EmailNotificationService> logger, IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    private string GetAppUrl() => string.IsNullOrWhiteSpace(_configuration["AppUrl"])
        ? "https://localhost:5173"
        : _configuration["AppUrl"]!.TrimEnd('/');

    public async Task SendWelcomeAsync(string toEmail, string userName, string loginEmail)
    {
        var subject = "Bem-vindo ao Diário de Classe! 🎓";
        var htmlBody = BuildWelcomeTemplate(userName, loginEmail, GetAppUrl());

        try
        {
            await _emailService.SendAsync(toEmail, userName, subject, htmlBody);
            _logger.LogInformation("Email de boas-vindas enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email de boas-vindas para {Email}", toEmail);
            throw;
        }
    }

    public async Task SendWelcomeProfessorAsync(string toEmail, string professorName, string escolaName, string loginEmail)
    {
        var subject = "Bem-vindo ao Diário de Classe, Professor! 👨‍🏫";
        var htmlBody = BuildWelcomeProfessorTemplate(professorName, escolaName, loginEmail, GetAppUrl());

        try
        {
            await _emailService.SendAsync(toEmail, professorName, subject, htmlBody);
            _logger.LogInformation("Email de boas-vindas de professor enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email de boas-vindas de professor para {Email}", toEmail);
            throw;
        }
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink)
    {
        var subject = "Confirme seu email - Diário de Classe";
        var htmlBody = BuildEmailConfirmationTemplate(userName, confirmationLink);

        try
        {
            await _emailService.SendAsync(toEmail, userName, subject, htmlBody);
            _logger.LogInformation("Email de confirmação enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email de confirmação para {Email}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Constrói template HTML de boas-vindas para novo usuário.
    /// </summary>
    private static string BuildWelcomeTemplate(string userName, string loginEmail, string appUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .welcome-text {{ font-size: 16px; margin: 20px 0; }}
        .cta-button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
        .cta-button:hover {{ background: #5568d3; }}
        .login-info {{ background: #e8f5e9; padding: 15px; border-radius: 5px; border-left: 4px solid #4caf50; margin: 20px 0; }}
        .login-info p {{ margin: 5px 0; }}
        .login-info strong {{ color: #2e7d32; }}
        .footer {{ text-align: center; padding: 20px; color: #999; font-size: 12px; border-top: 1px solid #ddd; margin-top: 20px; }}
        .feature-list {{ list-style: none; padding: 0; }}
        .feature-list li {{ padding: 10px 0; border-bottom: 1px solid #eee; }}
        .feature-list li:before {{ content: '✓ '; color: #4caf50; font-weight: bold; margin-right: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎓 Bem-vindo ao Diário de Classe!</h1>
        </div>
        <div class=""content"">
            <div class=""welcome-text"">
                <p>Olá <strong>{userName}</strong>,</p>
                <p>Sua conta foi criada com sucesso! Estamos felizes em tê-lo conosco.</p>
            </div>

            <div class=""login-info"">
                <p><strong>📧 Seu email de login:</strong></p>
                <p><code>{loginEmail}</code></p>
            </div>

            <div class=""welcome-text"">
                <p>Com o Diário de Classe, você pode:</p>
                <ul class=""feature-list"">
                    <li>Gerenciar notas e frequência</li>
                    <li>Comunicar-se com pais e alunos</li>
                    <li>Acompanhar o desempenho dos alunos</li>
                    <li>Organizar planos de aula</li>
                </ul>
            </div>

            <div style=""text-align: center;"">
                <a href=""{appUrl}/primeiro-acesso"" class=""cta-button"">Acessar Plataforma</a>
            </div>

            <div class=""welcome-text"" style=""font-size: 14px; color: #666; margin-top: 30px;"">
                <p>Se tiver dúvidas ou precisar de ajuda, acesse nossa <a href=""{appUrl}/ajuda"" style=""color: #667eea; text-decoration: none;"">central de suporte</a>.</p>
            </div>
        </div>
        <div class=""footer"">
            <p>© 2026 Diário de Classe. Todos os direitos reservados.</p>
            <p>Este é um email automático. Não responda este email.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Constrói template HTML de boas-vindas para novo professor.
    /// </summary>
    private static string BuildWelcomeProfessorTemplate(string professorName, string escolaName, string loginEmail, string appUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .welcome-text {{ font-size: 16px; margin: 20px 0; }}
        .cta-button {{ display: inline-block; background: #f5576c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
        .cta-button:hover {{ background: #e63e52; }}
        .info-box {{ background: #fff3e0; padding: 15px; border-radius: 5px; border-left: 4px solid #ff9800; margin: 20px 0; }}
        .info-box p {{ margin: 5px 0; }}
        .info-box strong {{ color: #e65100; }}
        .footer {{ text-align: center; padding: 20px; color: #999; font-size: 12px; border-top: 1px solid #ddd; margin-top: 20px; }}
        .feature-list {{ list-style: none; padding: 0; }}
        .feature-list li {{ padding: 10px 0; border-bottom: 1px solid #eee; }}
        .feature-list li:before {{ content: '→ '; color: #f5576c; font-weight: bold; margin-right: 10px; }}
        .school-highlight {{ background: #e3f2fd; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; }}
        .school-highlight p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>👨‍🏫 Bem-vindo, Professor!</h1>
        </div>
        <div class=""content"">
            <div class=""welcome-text"">
                <p>Olá <strong>{professorName}</strong>,</p>
                <p>Sua conta foi criada com sucesso e você já está pronto para começar!</p>
            </div>

            <div class=""school-highlight"">
                <p><strong>🏫 Escola:</strong></p>
                <p style=""font-size: 18px; color: #f5576c;"">{escolaName}</p>
            </div>

            <div class=""info-box"">
                <p><strong>📧 Seu email de login:</strong></p>
                <p><code>{loginEmail}</code></p>
                <p style=""font-size: 12px; margin-top: 10px; color: #666;"">Use este email para acessar a plataforma.</p>
            </div>

            <div class=""welcome-text"">
                <p><strong>Funcionalidades disponíveis:</strong></p>
                <ul class=""feature-list"">
                    <li>Registrar notas e frequência</li>
                    <li>Gerenciar suas disciplinas</li>
                    <li>Comunicar com pais e alunos</li>
                    <li>Visualizar relatórios de desempenho</li>
                    <li>Planejar aulas e atividades</li>
                </ul>
            </div>

            <div style=""text-align: center;"">
                <a href=""{appUrl}/primeiro-acesso"" class=""cta-button"">Acessar Minha Conta</a>
            </div>

            <div class=""welcome-text"" style=""font-size: 14px; color: #666; margin-top: 30px; background: #f0f0f0; padding: 15px; border-radius: 5px;"">
                <p><strong>💡 Primeira vez?</strong></p>
                <p>Confira nosso <a href=""{appUrl}/tutorial"" style=""color: #f5576c; text-decoration: none;"">tutorial de primeiros passos</a> para aprender como usar todas as funcionalidades.</p>
            </div>

            <div class=""welcome-text"" style=""font-size: 14px; color: #666;"">
                <p>Precisando de ajuda? Acesse nossa <a href=""{appUrl}/suporte-professor"" style=""color: #f5576c; text-decoration: none;"">central de suporte para professores</a>.</p>
            </div>
        </div>
        <div class=""footer"">
            <p>© 2026 Diário de Classe. Todos os direitos reservados.</p>
            <p>Este é um email automático. Não responda este email.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Constrói template HTML para confirmação de email.
    /// </summary>
    private static string BuildEmailConfirmationTemplate(string userName, string confirmationLink)
    {
        return $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .cta-button {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
        .cta-button:hover {{ background: #5568d3; }}
        .footer {{ text-align: center; padding: 20px; color: #999; font-size: 12px; border-top: 1px solid #ddd; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✓ Confirme seu Email</h1>
        </div>
        <div class=""content"">
            <p>Olá <strong>{userName}</strong>,</p>
            <p>Para ativar sua conta no Diário de Classe, clique no botão abaixo para confirmar seu email:</p>
            <div style=""text-align: center;"">
                <a href=""{confirmationLink}"" class=""cta-button"">Confirmar Email</a>
            </div>
            <p>Se não conseguir clicar no botão, copie este link e abra no seu navegador:</p>
            <p style=""word-break: break-all; background: #f0f0f0; padding: 10px; border-radius: 5px;"">{confirmationLink}</p>
            <p style=""color: #666; font-size: 14px;"">Este link expira em 24 horas.</p>
        </div>
        <div class=""footer"">
            <p>© 2026 Diário de Classe. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
    }
}