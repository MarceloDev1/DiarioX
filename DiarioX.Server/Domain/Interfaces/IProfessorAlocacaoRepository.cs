using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IProfessorAlocacaoRepository
{
    Task<ProfessorAlocacao?> GetByIdAsync(int id);
    Task<IEnumerable<ProfessorAlocacao>> GetByProfessorIdAsync(int professorId);
    Task<IEnumerable<ProfessorAlocacao>> GetAtivasAsync();
    Task SaveAsync(IEnumerable<ProfessorAlocacao> novas, IEnumerable<int> removerIds);
    Task DeleteAsync(ProfessorAlocacao alocacao);
}