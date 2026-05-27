namespace DiarioX.Server.Domain.Entities;

public class EmailTemplate
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Assunto { get; set; } = string.Empty;
    public string CorpoHtml { get; set; } = string.Empty;
    public string[] Variaveis { get; set; } = [];
    public bool Ativo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}
