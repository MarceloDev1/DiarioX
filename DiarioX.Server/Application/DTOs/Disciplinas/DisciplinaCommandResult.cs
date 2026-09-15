namespace DiarioX.Server.Application.DTOs.Disciplinas;

public enum DisciplinaResultError
{
    None, Validation, Conflict, NotFound
}

public record DisciplinaCommandResult(
    bool Success,
    string Message,
    DisciplinaResponse? Disciplina = null,
    DisciplinaResultError Error = DisciplinaResultError.None
);
