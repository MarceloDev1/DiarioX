using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IHorarioAulaRepository
{
    /// <summary>Tempos de aula da grade da turma, com a disciplina.</summary>
    Task<IReadOnlyList<HorarioAula>> ListByTurmaAsync(int turmaId);

    /// <summary>Tempos de aula das turmas informadas (sem navegações).</summary>
    Task<IReadOnlyList<HorarioAula>> ListByTurmasAsync(IReadOnlyCollection<int> turmaIds);

    /// <summary>Substitui a grade inteira da turma pelos tempos informados.</summary>
    Task SubstituirAsync(int turmaId, IReadOnlyCollection<HorarioAula> tempos);
}
