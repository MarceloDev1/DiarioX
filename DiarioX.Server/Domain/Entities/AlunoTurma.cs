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

    /// <summary>Vínculo encerrado pela transferência do aluno (RF014); não é um motivo escolhido na desenturmação.</summary>
    public const string MotivoTransferencia = "TRANSFERENCIA";

    /// <summary>Vínculo encerrado por remanejamento; o motivo digitado vai para a observação.</summary>
    public const string MotivoRemanejamento = "REMANEJAMENTO";

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

    /// <summary>
    /// Como o vínculo terminou: motivo da desenturmação (RF013), TRANSFERENCIA ou REMANEJAMENTO, com a
    /// observação ou o motivo digitado. Nulos nos vínculos encerrados antes de existirem.
    /// </summary>
    public string? MotivoDesenturmacao { get; set; }
    public string? ObservacaoDesenturmacao { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
