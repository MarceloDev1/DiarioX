namespace DiarioX.Server.Application.DTOs.Escolas;

public enum EscolaResultError
{
    None,
    Validation,
    Conflict,
    NotFound,
    UserNotFound
}

public record EscolaCommandResult(
    bool Success,
    string Message,
    EscolaResponse? Escola = null,
    EscolaResultError Error = EscolaResultError.None
);
