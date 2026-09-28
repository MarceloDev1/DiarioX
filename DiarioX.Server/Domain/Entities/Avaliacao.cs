namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Avaliação de uma turma em uma disciplina e período avaliativo (prova, trabalho...). A recuperação
/// do período é uma avaliação do tipo RECUPERACAO: fica fora da média e pode substituí-la.
/// </summary>
public class Avaliacao : ITenantEntity
{
    public const string TipoProva = "PROVA";
    public const string TipoTrabalho = "TRABALHO";
    public const string TipoAtividade = "ATIVIDADE";
    public const string TipoParticipacao = "PARTICIPACAO";
    public const string TipoOutro = "OUTRO";
    public const string TipoRecuperacao = "RECUPERACAO";

    public static readonly IReadOnlySet<string> Tipos = new HashSet<string>
    {
        TipoProva, TipoTrabalho, TipoAtividade, TipoParticipacao, TipoOutro, TipoRecuperacao,
    };

    public const int MaxNome = 100;
    public const decimal PesoMaximo = 100m;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;
    public int DisciplinaId { get; set; }
    public Disciplina Disciplina { get; set; } = null!;
    public int PeriodoAvaliativoId { get; set; }
    public PeriodoAvaliativo PeriodoAvaliativo { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = TipoProva;
    public DateOnly? Data { get; set; }

    /// <summary>Peso na média do período (regra de média ponderada).</summary>
    public decimal Peso { get; set; } = 1m;

    /// <summary>Pontos que a avaliação vale (regra de soma). Nulo nas regras de média.</summary>
    public decimal? ValorMaximo { get; set; }

    // Sem navegação, como na chamada: o Administrador global também lança notas.
    public int RegistradoPorUsuarioId { get; set; }
    public int? AtualizadoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<NotaAvaliacao> Notas { get; set; } = new List<NotaAvaliacao>();

    public bool EhRecuperacao => Tipo == TipoRecuperacao;
}

public class NotaAvaliacao : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int AvaliacaoId { get; set; }
    public Avaliacao Avaliacao { get; set; } = null!;
    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;
    public decimal Valor { get; set; }

    public int LancadaPorUsuarioId { get; set; }
    public DateTime LancadaEm { get; set; } = DateTime.UtcNow;
}
