namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed record EnturmacaoAtivaResponse(
    int AlunoId,
    int EscolaId,
    string EscolaNome,
    int TurmaId,
    string TurmaNome,
    int AnoLetivoId,
    int AnoReferencia,
    string Turno,
    DateOnly DataInicio);