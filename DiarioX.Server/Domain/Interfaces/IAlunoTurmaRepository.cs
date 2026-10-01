using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAlunoTurmaRepository
{
    Task<AlunoTurma?> GetAtivaByAlunoIdAsync(int alunoId);

    /// <summary>Enturmações ativas da turma, com o aluno. A turma deve ter sido validada pelo chamador.</summary>
    Task<IReadOnlyList<AlunoTurma>> GetAtivasByTurmaIdAsync(int turmaId);

    /// <summary>Enturmações ativas nas turmas do escopo do usuário, com o aluno e a turma (escola, modalidade e etapa).</summary>
    Task<IReadOnlyList<AlunoTurma>> GetAtivasAsync();

    Task<IReadOnlyList<int>> GetAlunoIdsComEnturmacaoAtivaAsync(IReadOnlyCollection<int> alunoIds);
    Task<bool> ExistsByAlunoIdAsync(int alunoId);
    Task<bool> HasVacancyAsync(int turmaId, DateOnly dataMovimentacao);

    /// <summary>Maior número de alunos na turma em qualquer data a partir de <paramref name="aPartirDe"/>.</summary>
    Task<int> GetOcupacaoMaximaAsync(int turmaId, DateOnly aPartirDe);

    /// <summary>Enturma os alunos em uma única transação; retorna false (sem gravar nada) se faltarem vagas.</summary>
    Task<bool> EnturmarAsync(IReadOnlyCollection<int> alunoIds, int turmaId, DateOnly dataInicio);

    Task RemanejarAsync(AlunoTurma vinculoOrigem, int turmaDestinoId, DateOnly dataMovimentacao);

    /// <summary>
    /// Encerra em uma única transação as enturmações ativas dos alunos na turma (o aluno deixa a turma
    /// em <paramref name="dataDesenturmacao"/>) e grava o novo status de cada um. Lança
    /// InvalidOperationException, sem gravar nada, se algum aluno não estiver mais enturmado nela.
    /// </summary>
    Task DesenturmarAsync(int turmaId, IReadOnlyDictionary<int, string> statusPorAluno, DateOnly dataDesenturmacao,
        string motivo, string? observacao);
}
