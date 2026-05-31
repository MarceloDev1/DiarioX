namespace DiarioX.Server.Application.DTOs.EtapasEnsino;

public record EtapaEnsinoResponse(
    int Id,
    int ModalidadeEnsinoId,
    string ModalidadeEnsinoNome,
    string Nome,
    string Sigla,
    int OrdemCronologica,
    int? IdadeRecomendada
);
