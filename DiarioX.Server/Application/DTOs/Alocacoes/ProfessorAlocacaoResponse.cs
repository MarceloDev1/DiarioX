namespace DiarioX.Server.Application.DTOs.Alocacoes;

public sealed record ProfessorAlocacaoResponse(
    int Id,
    int ProfessorId,
    string ProfessorNome,
    int TurmaId,
    string TurmaNome,
    int AnoLetivoId,
    int AnoReferencia,
    string Turno,
    int DisciplinaId,
    string DisciplinaNome,
    bool Ativa,
    string EscolaNome,
    string ModalidadeNome,
    string EtapaNome,
    string TurmaIdentificador
);

public sealed record ProfessorAlocacaoDisponibilidadeResponse(
    int TurmaId,
    string TurmaNome,
    int AnoLetivoId,
    int AnoReferencia,
    string Turno,
    int EscolaId,
    string EscolaNome,
    int ModalidadeEnsinoId,
    string ModalidadeNome,
    int EtapaEnsinoId,
    string EtapaNome,
    string TurmaIdentificador,
    List<ProfessorAlocacaoDisciplinaResponse> Disciplinas
);

public sealed record ProfessorAlocacaoDisciplinaResponse(
    int DisciplinaId,
    string DisciplinaNome,
    int? AlocacaoId,
    string? ProfessorAtualNome
);