using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAlunoTurmaRepository
{
    Task<AlunoTurma?> GetAtivaByAlunoIdAsync(int alunoId);
    Task<bool> ExistsByAlunoIdAsync(int alunoId);
    Task<bool> HasVacancyAsync(int turmaId, DateOnly dataMovimentacao);
    Task EnturmarAsync(int alunoId, int turmaId, DateOnly dataInicio);
    Task RemanejarAsync(AlunoTurma vinculoOrigem, int turmaDestinoId, DateOnly dataMovimentacao);
}