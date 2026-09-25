using DiarioX.Server.Application.DTOs.Professores;

namespace DiarioX.Server.Application.Interfaces;

public interface IProfessorService
{
    Task<ProfessorCommandResult> GetByIdAsync(int id);
    Task<IEnumerable<ProfessorResponse>> GetAllAsync();
    Task<IEnumerable<ProfessorResponse>> GetByEscolaIdAsync(int escolaId);
    Task<ProfessorCommandResult> CreateAsync(ProfessorRequest request);
    Task<ProfessorCommandResult> UpdateAsync(int id, ProfessorRequest request);
    Task<ProfessorCommandResult> UpdateSituacaoAsync(int id, ProfessorSituacaoRequest request);
    Task<ProfessorCommandResult> DeleteAsync(int id);
}
