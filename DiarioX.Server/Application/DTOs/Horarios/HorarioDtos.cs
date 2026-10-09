namespace DiarioX.Server.Application.DTOs.Horarios;

public record HorarioTurmaResumoResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    int AnoReferencia,
    string TipoFrequencia,
    int Tempos
);

/// <summary>Disciplina que pode ocupar a grade da turma, com os professores alocados nela.</summary>
public record HorarioDisciplinaResponse(int Id, string Nome, IReadOnlyList<string> Professores);

/// <summary>Um tempo de aula: dia da semana (1 = segunda ... 6 = sábado) e ordem no dia.</summary>
public record HorarioTempoDto(int DiaSemana, int Ordem, int DisciplinaId);

public record HorarioTurmaResponse(
    int TurmaId,
    string TurmaNome,
    string EscolaNome,
    int AnoReferencia,
    string TipoFrequencia,
    int MaxTempos,
    IReadOnlyList<HorarioDisciplinaResponse> Disciplinas,
    IReadOnlyList<HorarioTempoDto> Tempos
);

public record HorarioRequest(IReadOnlyList<HorarioTempoDto>? Tempos);

public enum HorarioResultError
{
    None,
    Validation,
    NotFound,
}

public record HorarioResult<T>(T? Value, string Message = "", HorarioResultError Error = HorarioResultError.None)
{
    public bool Success => Error == HorarioResultError.None;
}
