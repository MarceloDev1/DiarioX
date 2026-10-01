using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.DTOs.Turmas;

namespace DiarioX.Server.Application.Interfaces;

public interface IRemanejamentoAlunoService
{
    Task<EnturmacaoAtivaResponse?> GetEnturmacaoAtivaAsync(int alunoId);
    Task<VagasTurmaResponse?> GetVagasTurmaAsync(int turmaId, DateOnly data);
    Task<RemanejamentoAlunoResult> EnturmarAsync(int alunoId, EnturmacaoAlunoRequest request);
    Task<EnturmacaoLoteResult> EnturmarEmLoteAsync(EnturmacaoLoteRequest request);
    Task<RemanejamentoAlunoResult> RemanejarAsync(int alunoId, RemanejamentoAlunoRequest request);
    Task<RemanejamentoLoteResult> RemanejarEmLoteAsync(RemanejamentoLoteRequest request);

    /// <summary>Turmas que podem receber alunos da turma de origem, com vaga na data; nulo se a origem não existir.</summary>
    Task<IReadOnlyList<TurmaDestinoResponse>?> GetDestinosRemanejamentoAsync(int turmaOrigemId, DateOnly data);

    /// <summary>Alunos com enturmação ativa na turma; nulo se a turma não existir.</summary>
    Task<IReadOnlyList<AlunoEnturmadoResponse>?> GetAlunosEnturmadosAsync(int turmaId);

    Task<IReadOnlyList<EnturmacaoAtivaItemResponse>> GetEnturmacoesAtivasAsync();

    Task<DesenturmacaoResult> DesenturmarAsync(DesenturmacaoRequest request);
}
