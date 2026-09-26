namespace DiarioX.Server.Application.DTOs.Chamadas;

// ---------- Seleção de turma/disciplina ----------

public record ChamadaDisciplinaResponse(int Id, string Nome);

public record ChamadaPeriodoResponse(int Id, string Nome, DateOnly DataInicio, DateOnly DataTermino);

public record ChamadaTurmaResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    int AnoReferencia,
    string Turno,
    DateOnly AnoLetivoInicio,
    DateOnly AnoLetivoTermino,
    IReadOnlyList<ChamadaDisciplinaResponse> Disciplinas,
    IReadOnlyList<ChamadaPeriodoResponse> Periodos
);

// ---------- Lançamento ----------

public record ChamadaAlunoResponse(
    int AlunoId,
    string Matricula,
    string Nome,
    string? Situacao,
    string? Justificativa
);

/// <summary>Chamada de uma data: a existente (ChamadaId preenchido) ou a lista em branco para lançar.</summary>
public record ChamadaResponse(
    int? ChamadaId,
    int TurmaId,
    int DisciplinaId,
    DateOnly Data,
    int QuantidadeAulas,
    string? Conteudo,
    string? RegistradoPor,
    DateTime? RegistradoEm,
    string? AtualizadoPor,
    DateTime? AtualizadoEm,
    IReadOnlyList<ChamadaAlunoResponse> Alunos
);

public class ChamadaAlunoRequest
{
    public int AlunoId { get; set; }
    public string Situacao { get; set; } = string.Empty;
    public string? Justificativa { get; set; }
}

public class ChamadaRequest
{
    public int TurmaId { get; set; }
    public int DisciplinaId { get; set; }
    public DateOnly Data { get; set; }
    public int QuantidadeAulas { get; set; } = 1;
    public string? Conteudo { get; set; }
    public List<ChamadaAlunoRequest> Alunos { get; set; } = new();
}

// ---------- Histórico e frequência ----------

public record ChamadaResumoResponse(
    int Id,
    DateOnly Data,
    int QuantidadeAulas,
    int Presentes,
    int Faltas,
    int FaltasJustificadas,
    string? Conteudo,
    string? RegistradoPor,
    DateTime RegistradoEm
);

public record FrequenciaAlunoResponse(
    int AlunoId,
    string Matricula,
    string Nome,
    int Aulas,
    int Faltas,
    int FaltasJustificadas,
    decimal? PercentualFrequencia,
    bool AbaixoDoMinimo
);

public record FrequenciaResponse(
    DateOnly De,
    DateOnly Ate,
    int AulasDadas,
    decimal FrequenciaMinima,
    IReadOnlyList<FrequenciaAlunoResponse> Alunos
);

// ---------- Resultado ----------

public enum ChamadaResultError
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public record ChamadaCommandResult(
    bool Success,
    string Message,
    ChamadaResponse? Chamada = null,
    ChamadaResultError Error = ChamadaResultError.None
);

public record ChamadaQueryResult<T>(
    T? Value,
    string Message = "",
    ChamadaResultError Error = ChamadaResultError.None
)
{
    public bool Success => Error == ChamadaResultError.None;
}
