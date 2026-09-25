namespace DiarioX.Server.Domain.Entities;

public class Professor : ITenantEntity
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";
    public const string StatusAfastado = "AFASTADO";
    public const string StatusLicenciado = "LICENCIADO";

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? UsuarioId { get; set; }
    public int EscolaId { get; set; }

    // Dados Pessoais
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;

    // Dados Contratuais
    public string Matricula { get; set; } = string.Empty;
    public DateTime DataAdmissao { get; set; }
    public string Situacao { get; set; } = StatusAtivo;

    // Relacionamentos
    public User? Usuario { get; set; }
    public Escola Escola { get; set; } = null!;
    public ICollection<ProfessorDisciplina> ProfessorDisciplinas { get; set; } = new List<ProfessorDisciplina>();

    // Auditoria
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
