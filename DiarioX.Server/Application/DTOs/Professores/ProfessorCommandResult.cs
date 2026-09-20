namespace DiarioX.Server.Application.DTOs.Professores;

public enum ProfessorResultError
{
    None,
    Validation,
    Conflict,
    NotFound,
    DependencyNotFound
}

public record ProfessorCommandResult(
    bool Success,
    string Message,
    ProfessorResponse? Professor = null,
    ProfessorResultError Error = ProfessorResultError.None
);
