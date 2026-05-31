namespace DiarioX.Server.Application.DTOs.AnosLetivos;

public record PeriodoAvaliativoResponse(
    int Id,
    int AnoLetivoId,
    string Nome,
    int Numero,
    DateOnly DataInicio,
    DateOnly DataTermino
);

public record AnoLetivoResponse(
    int Id,
    int AnoReferencia,
    DateOnly DataInicio,
    DateOnly DataTermino,
    string TipoPeriodo,
    List<PeriodoAvaliativoResponse> Periodos
);
