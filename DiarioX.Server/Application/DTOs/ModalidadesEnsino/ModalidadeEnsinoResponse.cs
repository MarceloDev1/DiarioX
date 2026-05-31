namespace DiarioX.Server.Application.DTOs.ModalidadesEnsino;

public record ModalidadeEnsinoResponse(
    int Id,
    string Nome,
    string Sigla,
    string? CodigoMec,
    string? Descricao,
    string Status
);
