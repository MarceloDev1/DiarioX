using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAlunoTurmaRepository
{
    Task<AlunoTurma?> GetAtivaByAlunoIdAsync(int alunoId);
    Task<IReadOnlyList<int>> GetAlunoIdsComEnturmacaoAtivaAsync(IReadOnlyCollection<int> alunoIds);
    Task<bool> ExistsByAlunoIdAsync(int alunoId);
    Task<bool> HasVacancyAsync(int turmaId, DateOnly dataMovimentacao);

    /// <summary>Maior número de alunos na turma em qualquer data a partir de <paramref name="aPartirDe"/>.</summary>
    Task<int> GetOcupacaoMaximaAsync(int turmaId, DateOnly aPartirDe);

    /// <summary>Enturma os alunos em uma única transação; retorna false (sem gravar nada) se faltarem vagas.</summary>
    Task<bool> EnturmarAsync(IReadOnlyCollection<int> alunoIds, int turmaId, DateOnly dataInicio);

    Task RemanejarAsync(AlunoTurma vinculoOrigem, int turmaDestinoId, DateOnly dataMovimentacao);
}
