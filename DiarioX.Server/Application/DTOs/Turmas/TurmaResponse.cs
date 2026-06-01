namespace DiarioX.Server.Application.DTOs.Turmas;

public record TurmaResponse(
    int Id,
    int AnoLetivoId,
    int AnoReferencia,
    int EscolaId,
    string EscolaNome,
    int ModalidadeEnsinoId,
    string ModalidadeEnsinoNome,
    int EtapaEnsinoId,
    string EtapaEnsinoNome,
    string NomeIdentificador,
    string NomeCompleto,
    string Turno,
    string TurnoDescricao,
    int VagasOfertadas,
    string Status
);