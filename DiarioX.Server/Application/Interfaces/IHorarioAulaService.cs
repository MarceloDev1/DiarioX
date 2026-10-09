using DiarioX.Server.Application.DTOs.Horarios;

namespace DiarioX.Server.Application.Interfaces;

public interface IHorarioAulaService
{
    /// <summary>Turmas ativas visíveis ao usuário, com a quantidade de tempos já montados na grade.</summary>
    Task<IReadOnlyList<HorarioTurmaResumoResponse>> GetTurmasAsync();

    Task<HorarioResult<HorarioTurmaResponse>> GetAsync(int turmaId);

    /// <summary>Substitui a grade semanal da turma.</summary>
    Task<HorarioResult<HorarioTurmaResponse>> SalvarAsync(int turmaId, HorarioRequest request);
}
