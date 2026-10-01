namespace DiarioX.Server.Application.DTOs.Alunos;

public sealed record EnturmacaoLoteFalha(int AlunoId, string Motivo);

/// <summary>
/// Resultado da enturmação em lote. Em caso de falha nada é gravado; <see cref="Falhas"/> lista
/// os alunos que impediram o lote e o motivo de cada um.
/// </summary>
public sealed record EnturmacaoLoteResult(
    bool Success,
    string Message,
    AlunoResultError? Error = null,
    IReadOnlyList<EnturmacaoLoteFalha>? Falhas = null);
