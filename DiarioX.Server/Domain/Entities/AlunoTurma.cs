namespace DiarioX.Server.Domain.Entities;

public class AlunoTurma : ITenantEntity
{
    public const string MotivoReestruturacaoInterna = "REESTRUTURACAO_INTERNA";
    public const string MotivoNaoCompareceu = "NAO_COMPARECEU";
    public const string MotivoFalecimento = "FALECIMENTO";
    public const string MotivoErroMatricula = "ERRO_MATRICULA_ENTURMACAO";
    public const string MotivoOutros = "OUTROS";

    public static readonly IReadOnlySet<string> MotivosDesenturmacao = new HashSet<string>
    {
        MotivoReestruturacaoInterna, MotivoNaoCompareceu, MotivoFalecimento, MotivoErroMatricula, MotivoOutros,
    };

    public const int MaxObservacaoDesenturmacao = 500;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;
    public DateOnly DataInicio { get; set; }

    /// <summary>
    /// Último dia do aluno na turma (nulo = enturmação ativa). Um vínculo desfeito no mesmo dia em que
    /// começaria termina na véspera do início (DataFim = DataInicio - 1): fica no histórico, com o motivo,
    /// mas não vale para nenhuma data. Consultas por intervalo precisam descartá-lo.
    /// </summary>
    public DateOnly? DataFim { get; set; }

    /// <summary>Preenchidos quando o vínculo é encerrado por desenturmação (RF013); nulos no remanejamento.</summary>
    public string? MotivoDesenturmacao { get; set; }
    public string? ObservacaoDesenturmacao { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
