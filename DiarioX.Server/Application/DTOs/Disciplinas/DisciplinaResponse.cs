namespace DiarioX.Server.Application.DTOs.Disciplinas;

public record DisciplinaEtapaEnsinoResponse(
    int Id,
    string EtapaEnsinoNome
);

public record DisciplinaResponse(
    int Id,
    string Nome,
    string Codigo,
    string Descricao,
    bool Ativa,
    List<DisciplinaEtapaEnsinoResponse> EtapasEnsino
);
