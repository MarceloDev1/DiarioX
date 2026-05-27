namespace DiarioX.Server.Domain.Entities;

public class EmailLog
{
    public const string StatusPendente = "PENDENTE";
    public const string StatusEnviado = "ENVIADO";
    public const string StatusFalhou = "FALHOU";

    public int Id { get; set; }
    public int? TemplateId { get; set; }
    public string DestinatarioEmail { get; set; } = string.Empty;
    public string? DestinatarioNome { get; set; }
    public string Assunto { get; set; } = string.Empty;
    public string CorpoHtml { get; set; } = string.Empty;
    public string Status { get; set; } = StatusPendente;
    public int Tentativas { get; set; } = 0;
    public string? ErroMensagem { get; set; }
    public DateTime? EnviadoEm { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public EmailTemplate? Template { get; set; }
}
