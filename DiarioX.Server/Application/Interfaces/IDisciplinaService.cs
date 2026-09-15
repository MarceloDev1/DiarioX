using DiarioX.Server.Application.DTOs.Disciplinas;

namespace DiarioX.Server.Application.Interfaces;

public interface IDisciplinaService
{
    Task<IEnumerable<DisciplinaResponse>> GetAllAsync();
    Task<DisciplinaResponse?> GetByIdAsync(int id);
    Task<DisciplinaCommandResult> CreateAsync(DisciplinaRequest request);
    Task<DisciplinaCommandResult> UpdateAsync(int id, DisciplinaRequest request);
    Task<DisciplinaCommandResult> DeleteAsync(int id);
}
