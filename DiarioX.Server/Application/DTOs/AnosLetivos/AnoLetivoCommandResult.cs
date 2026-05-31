namespace DiarioX.Server.Application.DTOs.AnosLetivos;

public enum AnoLetivoResultError
{
    None, Validation, Conflict, NotFound
}

public record AnoLetivoCommandResult(
    bool Success,
    string Message,
    AnoLetivoResponse? AnoLetivo = null,
    AnoLetivoResultError Error = AnoLetivoResultError.None
);
