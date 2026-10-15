namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Saída do aluno da escola (RF014): para outra instituição ou, entre escolas da rede, para outra escola
/// da mesma instituição. Guarda os dados da Declaração de Transferência, que pode ser emitida de novo.
/// </summary>
public class Transferencia : ITenantEntity
{
    public const string TipoOutraRede = "OUTRA_REDE";
    public const string TipoEntreEscolasDaRede = "ENTRE_ESCOLAS_DA_REDE";
    public const string TipoMudancaMunicipioEstado = "MUDANCA_MUNICIPIO_ESTADO";

    /// <summary>Tipos aceitos, com o texto usado na declaração.</summary>
    public static readonly IReadOnlyDictionary<string, string> Tipos = new Dictionary<string, string>
    {
        [TipoOutraRede] = "Transferência para Outra Rede",
        [TipoEntreEscolasDaRede] = "Transferência Entre Escolas da Rede",
        [TipoMudancaMunicipioEstado] = "Mudança de Município/Estado",
    };

    public const int MaxEscolaDestino = 200;
    public const int MaxMotivo = 500;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    /// <summary>Escola que o aluno deixa: a da turma, se estava enturmado; senão, a do cadastro.</summary>
    public int EscolaOrigemId { get; set; }
    public Escola EscolaOrigem { get; set; } = null!;

    /// <summary>Turma encerrada pela transferência; nula se o aluno aguardava enturmação.</summary>
    public int? TurmaId { get; set; }
    public Turma? Turma { get; set; }

    public int AnoLetivoId { get; set; }
    public AnoLetivo AnoLetivo { get; set; } = null!;

    /// <summary>Data do desligamento: a partir dela o aluno não figura mais no diário da turma.</summary>
    public DateOnly DataTransferencia { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string EscolaDestino { get; set; } = string.Empty;

    /// <summary>
    /// Escola da rede que recebe o aluno (só na transferência entre escolas da rede). O aluno passa a
    /// pertencer a ela aguardando enturmação; na escola de origem ele continua listado como transferido.
    /// </summary>
    public int? EscolaDestinoId { get; set; }
    public Escola? EscolaDestinoRede { get; set; }

    public string? Motivo { get; set; }

    // Sem navegação, como em Chamada: o Administrador global não pertence à instituição.
    public int RegistradoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
