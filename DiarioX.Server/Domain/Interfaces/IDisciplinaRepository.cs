using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IDisciplinaRepository
{
    Task<Disciplina?> GetByIdAsync(int id);
    Task<IEnumerable<Disciplina>> GetAllAsync();
    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId = null);
    Task<bool> ExistsByNomeAsync(string nome, int? excludeId = null);
    Task<Disciplina> AddAsync(Disciplina disciplina);
    Task UpdateAsync(Disciplina disciplina);
    Task DeleteAsync(int id);
}
