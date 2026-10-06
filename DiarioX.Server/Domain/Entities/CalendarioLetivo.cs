namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Calendário Letivo de um ano letivo (RF005A): o da rede (EscolaId nulo), que vale para todas as escolas,
/// ou o de uma escola, cujos eventos substituem os da rede nos mesmos dias. Só o calendário publicado
/// trava os lançamentos do diário de classe.
/// </summary>
public class CalendarioLetivo : ITenantEntity
{
    /// <summary>Mínimo de dias de efetivo trabalho escolar por ano (LDB, art. 24, I).</summary>
    public const int MetaDiasLetivos = 200;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int AnoLetivoId { get; set; }
    public AnoLetivo AnoLetivo { get; set; } = null!;

    /// <summary>Nulo = calendário da rede (Rede/Geral).</summary>
    public int? EscolaId { get; set; }
    public Escola? Escola { get; set; }

    public DateTime? PublicadoEm { get; set; }

    // Sem navegação, como em Chamada: o Administrador global não pertence à instituição.
    public int? PublicadoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EventoCalendario> Eventos { get; set; } = new List<EventoCalendario>();

    public bool Publicado => PublicadoEm is not null;

    /// <summary>Dia útil (segunda a sexta): é letivo quando não há evento no dia.</summary>
    public static bool DiaUtil(DateOnly data) => data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}

/// <summary>
/// Evento de um dia do calendário. Um intervalo (ex.: recesso de 10/07 a 20/07) é gravado como um evento por
/// dia, o que permite alterar ou remover dias isolados depois.
/// </summary>
public class EventoCalendario : ITenantEntity
{
    public const string TipoFeriado = "FERIADO";
    public const string TipoRecesso = "RECESSO";
    public const string TipoPontoFacultativo = "PONTO_FACULTATIVO";
    public const string TipoConselhoClasse = "CONSELHO_CLASSE";
    public const string TipoPlantaoPedagogico = "PLANTAO_PEDAGOGICO";
    public const string TipoFormacaoContinuada = "FORMACAO_CONTINUADA";
    public const string TipoSabadoLetivo = "SABADO_LETIVO";

    public static readonly IReadOnlyDictionary<string, string> Tipos = new Dictionary<string, string>
    {
        [TipoFeriado] = "Feriado",
        [TipoRecesso] = "Recesso Escolar",
        [TipoPontoFacultativo] = "Ponto Facultativo",
        [TipoConselhoClasse] = "Conselho de Classe",
        [TipoPlantaoPedagogico] = "Plantão Pedagógico",
        [TipoFormacaoContinuada] = "Formação Continuada",
        [TipoSabadoLetivo] = "Dia Letivo Especial (Sábado Letivo)",
    };

    public const int MaxDescricao = 150;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CalendarioLetivoId { get; set; }
    public CalendarioLetivo CalendarioLetivo { get; set; } = null!;
    public DateOnly Data { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Considerado dia letivo: permite (true) ou bloqueia (false) o registro de aula.</summary>
    public bool ComAula { get; set; }
}
