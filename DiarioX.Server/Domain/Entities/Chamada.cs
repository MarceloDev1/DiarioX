namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Chamada de uma aula: turma + disciplina + data. Uma aula dupla é registrada uma única vez com
/// QuantidadeAulas = 2, e a situação de cada aluno vale para todas as aulas do registro.
/// </summary>
public class Chamada : ITenantEntity
{
    public const int MaxQuantidadeAulas = 6;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;
    public int DisciplinaId { get; set; }
    public Disciplina Disciplina { get; set; } = null!;
    public DateOnly Data { get; set; }
    public int QuantidadeAulas { get; set; } = 1;

    /// <summary>Conteúdo ministrado na aula, como no diário de classe.</summary>
    public string? Conteudo { get; set; }

    // Sem navegação: o Administrador global (usuário sem instituição) também lança chamadas,
    // e o filtro de tenant de User o esconderia num Include.
    public int RegistradoPorUsuarioId { get; set; }
    public int? AtualizadoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ChamadaAluno> Registros { get; set; } = new List<ChamadaAluno>();
}

public class ChamadaAluno : ITenantEntity
{
    public const string SituacaoPresente = "PRESENTE";
    public const string SituacaoFalta = "FALTA";
    public const string SituacaoFaltaJustificada = "FALTA_JUSTIFICADA";

    public static readonly IReadOnlySet<string> Situacoes =
        new HashSet<string> { SituacaoPresente, SituacaoFalta, SituacaoFaltaJustificada };

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ChamadaId { get; set; }
    public Chamada Chamada { get; set; } = null!;
    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;
    public string Situacao { get; set; } = SituacaoPresente;

    /// <summary>Motivo da falta justificada (atestado, etc.).</summary>
    public string? Justificativa { get; set; }
}
