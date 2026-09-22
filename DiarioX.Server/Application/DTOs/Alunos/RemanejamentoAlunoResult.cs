namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed record RemanejamentoAlunoResult(bool Success, string Message, AlunoResultError? Error = null);