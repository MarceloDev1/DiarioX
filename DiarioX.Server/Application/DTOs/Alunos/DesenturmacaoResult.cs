namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed record DesenturmacaoFalha(int AlunoId, string Motivo);

/// <summary>
/// Resultado da desenturmação. Em caso de falha nada é gravado; <see cref="Falhas"/> lista
/// os alunos que impediram a operação e o motivo de cada um.
/// </summary>
public sealed record DesenturmacaoResult(
    bool Success,
    string Message,
    AlunoResultError? Error = null,
    IReadOnlyList<DesenturmacaoFalha>? Falhas = null);
