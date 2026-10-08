using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.DTOs.Conteudos;

// ---------- Seleção de turma/disciplina ----------

public record ConteudoDisciplinaResponse(int Id, string Nome);

public record ConteudoPeriodoResponse(int Id, string Nome, DateOnly DataInicio, DateOnly DataTermino);

public record ConteudoTurmaResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    int AnoReferencia,
    DateOnly AnoLetivoInicio,
    DateOnly AnoLetivoTermino,
    // Disciplinas em que o usuário pode registrar conteúdo (a grade da turma, ou as alocações do professor).
    IReadOnlyList<ConteudoDisciplinaResponse> Disciplinas,
    IReadOnlyList<ConteudoPeriodoResponse> Periodos,
    // POR_AULA: o diário confere a frequência da disciplina. DIARIA: a frequência é da turma no dia.
    string TipoFrequencia = EtapaEnsino.FrequenciaPorAula
);

// ---------- Registro ----------

public record HabilidadeResumoResponse(int Id, string Codigo, string Descricao, bool Ativa = true);

/// <summary>Conteúdo de uma data: o registrado (ConteudoId preenchido) ou o formulário em branco.</summary>
public record ConteudoMinistradoResponse(
    int? ConteudoId,
    int TurmaId,
    int DisciplinaId,
    string DisciplinaNome,
    DateOnly Data,
    string? Descricao,
    IReadOnlyList<HabilidadeResumoResponse> Habilidades,
    string? RegistradoPor,
    DateTime? RegistradoEm,
    string? AtualizadoPor,
    DateTime? AtualizadoEm,
    // EX01: motivo do bloqueio da data pelo Calendário Letivo; nulo = registro permitido.
    string? Bloqueio = null
);

public class ConteudoMinistradoRequest
{
    public int TurmaId { get; set; }
    public int DisciplinaId { get; set; }
    public DateOnly Data { get; set; }
    public string? Descricao { get; set; }
    public List<int> HabilidadesIds { get; set; } = new();
}

// ---------- Diário (RN01) ----------

public record DiarioFrequenciaResponse(int ChamadaId, int QuantidadeAulas, int Presentes, int Faltas, int FaltasJustificadas);

public record DiarioConteudoResponse(
    int Id,
    int DisciplinaId,
    string DisciplinaNome,
    string Descricao,
    IReadOnlyList<HabilidadeResumoResponse> Habilidades
);

public static class DiarioPendencia
{
    public const string SemConteudo = "SEM_CONTEUDO";
    public const string SemFrequencia = "SEM_FREQUENCIA";
}

/// <summary>Um dia do diário. <see cref="Pendencia"/> preenchida = divergência entre frequência e conteúdo (RN01).</summary>
public record DiarioDiaResponse(
    DateOnly Data,
    DiarioFrequenciaResponse? Frequencia,
    IReadOnlyList<DiarioConteudoResponse> Conteudos,
    string? Pendencia,
    string? Alerta
);

public record DiarioResponse(
    string TipoFrequencia,
    DateOnly De,
    DateOnly Ate,
    int TotalPendencias,
    IReadOnlyList<DiarioDiaResponse> Dias
);

// ---------- Resultado ----------

public enum ConteudoResultError
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public record ConteudoCommandResult(
    bool Success,
    string Message,
    ConteudoMinistradoResponse? Conteudo = null,
    ConteudoResultError Error = ConteudoResultError.None
);

public record ConteudoQueryResult<T>(
    T? Value,
    string Message = "",
    ConteudoResultError Error = ConteudoResultError.None
)
{
    public bool Success => Error == ConteudoResultError.None;
}
