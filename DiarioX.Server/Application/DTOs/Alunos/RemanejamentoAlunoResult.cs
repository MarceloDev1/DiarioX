namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed record RemanejamentoAlunoResult(bool Success, string Message, AlunoResultError? Error = null);

public sealed record RemanejamentoFalha(int AlunoId, string Motivo);

/// <summary>
/// Resultado do remanejamento em massa. Em caso de falha nada é gravado; <see cref="Falhas"/> lista
/// os alunos que impediram a operação e o motivo de cada um.
/// </summary>
public sealed record RemanejamentoLoteResult(
    bool Success,
    string Message,
    AlunoResultError? Error = null,
    IReadOnlyList<RemanejamentoFalha>? Falhas = null);
