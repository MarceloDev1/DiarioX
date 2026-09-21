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
    bool Ativa
);

public sealed record ProfessorAlocacaoDisponibilidadeResponse(
    int TurmaId,
    string TurmaNome,
    int AnoLetivoId,
    int AnoReferencia,
    string Turno,
    List<ProfessorAlocacaoDisciplinaResponse> Disciplinas
);

public sealed record ProfessorAlocacaoDisciplinaResponse(
    int DisciplinaId,
    string DisciplinaNome,
    int? AlocacaoId,
    string? ProfessorAtualNome
);