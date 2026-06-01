namespace DiarioX.Server.Application.DTOs.Turmas;

public enum TurmaResultError
{
    None, Validation, Conflict, NotFound
}

public record TurmaCommandResult(
    bool Success,
    string Message,
    TurmaResponse? Turma = null,
    TurmaResultError Error = TurmaResultError.None
);