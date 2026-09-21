namespace DiarioX.Server.Application.DTOs.Alunos;

public enum AlunoResultError
{
    None,
    Validation,
    Conflict,
    NotFound,
    DependencyNotFound
}

public record AlunoCommandResult(
    bool Success,
    string Message,
    AlunoResponse? Aluno = null,
    AlunoResultError Error = AlunoResultError.None
);
