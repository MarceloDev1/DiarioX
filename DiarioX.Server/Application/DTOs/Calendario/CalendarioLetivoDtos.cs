namespace DiarioX.Server.Application.DTOs.Calendario;

// ---------- Seleção de ano letivo e escola ----------

public record CalendarioAnoOpcao(int Id, int AnoReferencia, DateOnly DataInicio, DateOnly DataTermino);

public record CalendarioEscolaOpcao(int Id, string Nome);

public record CalendarioOpcoesResponse(
    IReadOnlyList<CalendarioAnoOpcao> AnosLetivos,
    IReadOnlyList<CalendarioEscolaOpcao> Escolas,
    // Usuário vinculado a escolas específicas consulta o calendário da rede, mas só altera o da escola.
    bool PodeEditarRede
);

// ---------- Calendário ----------

public record CalendarioPeriodoResponse(int Id, string Nome, int Numero, DateOnly DataInicio, DateOnly DataTermino);

public record EventoCalendarioResponse(
    DateOnly Data,
    string Tipo,
    string TipoNome,
    string Descricao,
    bool ComAula,
    // No calendário de uma escola: evento da rede (publicado) sem evento da escola no mesmo dia.
    bool Herdado
);

public record CalendarioLetivoResponse(
    int AnoLetivoId,
    int AnoReferencia,
    DateOnly DataInicio,
    DateOnly DataTermino,
    string TipoPeriodo,
    IReadOnlyList<CalendarioPeriodoResponse> Periodos,
    int? EscolaId,
    string? EscolaNome,
    bool Publicado,
    DateTime? PublicadoEm,
    // Calendário de escola: se o da rede está publicado (só então os eventos dele são herdados).
    bool RedePublicada,
    int DiasLetivos,
    int MetaDiasLetivos,
    // Escopo de escolas do usuário; a permissão calendario-letivo.editar é checada à parte.
    bool PodeEditar,
    IReadOnlyList<EventoCalendarioResponse> Eventos
);

// ---------- Edição ----------

public class EventoCalendarioRequest
{
    public int AnoLetivoId { get; set; }
    public int? EscolaId { get; set; }
    public DateOnly DataInicio { get; set; }

    /// <summary>Último dia do intervalo (cadastro em lote); nulo = só DataInicio.</summary>
    public DateOnly? DataFim { get; set; }
    public string? Tipo { get; set; }
    public string? Descricao { get; set; }
    public bool? ComAula { get; set; }
}

public class CalendarioPublicacaoRequest
{
    public int AnoLetivoId { get; set; }
    public int? EscolaId { get; set; }
}

// ---------- Resultado ----------

public enum CalendarioResultError
{
    None,
    Validation,
    NotFound,
    Forbidden
}

public record CalendarioCommandResult(
    bool Success,
    string Message,
    CalendarioLetivoResponse? Calendario = null,
    CalendarioResultError Error = CalendarioResultError.None
);
