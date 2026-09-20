namespace DiarioX.Server.Application.Interfaces;

/// <summary>
/// Serviço de notificações por email com templates pré-formatados.
/// Abstrai os templates HTML e fornece métodos tipados para cada tipo de notificação.
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Envia email de boas-vindas para novo usuário.
    /// </summary>
    /// <param name="toEmail">Email do usuário</param>
    /// <param name="userName">Nome do usuário</param>
    /// <param name="loginEmail">Email de login (pode ser diferente do userName)</param>
    /// <returns>Task com resultado do envio</returns>
    Task SendWelcomeAsync(string toEmail, string userName, string loginEmail);

    /// <summary>
    /// Envia email de boas-vindas para novo professor.
    /// </summary>
    /// <param name="toEmail">Email do professor</param>
    /// <param name="professorName">Nome do professor</param>
    /// <param name="escolaName">Nome da escola</param>
    /// <param name="loginEmail">Email de login</param>
    /// <returns>Task com resultado do envio</returns>
    Task SendWelcomeProfessorAsync(string toEmail, string professorName, string escolaName, string loginEmail);

    /// <summary>
    /// Envia email de confirmação de cadastro.
    /// </summary>
    /// <param name="toEmail">Email para confirmação</param>
    /// <param name="userName">Nome do usuário</param>
    /// <param name="confirmationLink">Link de confirmação</param>
    /// <returns>Task com resultado do envio</returns>
    Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink);
}
