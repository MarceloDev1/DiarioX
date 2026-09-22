using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAlunoTurmaRepository
{
    Task<AlunoTurma?> GetAtivaByAlunoIdAsync(int alunoId);
    Task<bool> HasVacancyAsync(int turmaId, DateOnly dataMovimentacao);
    Task RemanejarAsync(AlunoTurma vinculoOrigem, int turmaDestinoId, DateOnly dataMovimentacao);
}