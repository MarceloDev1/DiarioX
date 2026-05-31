namespace DiarioX.Server.Application.DTOs.EtapasEnsino;

public enum EtapaEnsinoResultError
{
    None, Validation, Conflict, NotFound
}

public record EtapaEnsinoCommandResult(
    bool Success,
    string Message,
    EtapaEnsinoResponse? EtapaEnsino = null,
    EtapaEnsinoResultError Error = EtapaEnsinoResultError.None
);
