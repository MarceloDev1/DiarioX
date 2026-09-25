using DiarioX.Server.Application.DTOs.Alunos;

namespace DiarioX.Server.Application.Interfaces;

public interface IAlunoService
{
    Task<AlunoCommandResult> GetByIdAsync(int id);
    Task<IEnumerable<AlunoResponse>> GetAllAsync();
    Task<AlunoCommandResult> CreateAsync(AlunoRequest request);
    Task<AlunoCommandResult> UpdateAsync(int id, AlunoRequest request);
    Task<AlunoCommandResult> UpdateStatusAsync(int id, AlunoStatusRequest request);
    Task<AlunoCommandResult> DeleteAsync(int id);
}
