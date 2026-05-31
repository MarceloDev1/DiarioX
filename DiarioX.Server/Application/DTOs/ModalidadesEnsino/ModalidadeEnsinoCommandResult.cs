namespace DiarioX.Server.Application.DTOs.ModalidadesEnsino;

public enum ModalidadeEnsinoResultError
{
    None, Validation, Conflict, NotFound
}

public record ModalidadeEnsinoCommandResult(
    bool Success,
    string Message,
    ModalidadeEnsinoResponse? ModalidadeEnsino = null,
    ModalidadeEnsinoResultError Error = ModalidadeEnsinoResultError.None
);
