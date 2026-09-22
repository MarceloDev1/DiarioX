using DiarioX.Server.Application.DTOs.Alunos;

namespace DiarioX.Server.Application.Interfaces;

public interface IRemanejamentoAlunoService
{
    Task<EnturmacaoAtivaResponse?> GetEnturmacaoAtivaAsync(int alunoId);
    Task<RemanejamentoAlunoResult> RemanejarAsync(int alunoId, RemanejamentoAlunoRequest request);
}