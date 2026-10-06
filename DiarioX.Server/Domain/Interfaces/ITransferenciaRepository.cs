using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface ITransferenciaRepository
{
    /// <summary>Transferência com aluno, escola de origem, turma e ano letivo (para a declaração).</summary>
    Task<Transferencia?> GetByIdAsync(int id);

    /// <summary>
    /// Todas as transferências do escopo do usuário, da mais recente para a mais antiga, com aluno, escola de
    /// origem e, se havia turma, a turma com modalidade e etapa (para a lista de alunos transferidos).
    /// </summary>
    Task<IReadOnlyList<Transferencia>> ListAsync();

    /// <summary>Transferências do aluno, da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<Transferencia>> GetByAlunoIdAsync(int alunoId);

    /// <summary>Se o aluno tem transferência registrada em qualquer escola da instituição.</summary>
    Task<bool> ExistsByAlunoIdAsync(int alunoId);

    /// <summary>
    /// Em uma única transação: encerra a enturmação ativa do aluno (o aluno deixa a turma na data da
    /// transferência, liberando a vaga), muda o status para TRANSFERIDO e grava a transferência.
    /// Lança InvalidOperationException, sem gravar nada, se o aluno já foi transferido ou se a
    /// enturmação ativa não é mais a informada em <paramref name="vinculoEsperadoId"/>.
    /// </summary>
    Task<Transferencia> TransferirAsync(Transferencia transferencia, int? vinculoEsperadoId);
}
