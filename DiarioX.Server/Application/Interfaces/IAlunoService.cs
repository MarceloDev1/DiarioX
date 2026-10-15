using DiarioX.Server.Application.DTOs.Alunos;

namespace DiarioX.Server.Application.Interfaces;

public interface IAlunoService
{
    Task<AlunoCommandResult> GetByIdAsync(int id);
    Task<IEnumerable<AlunoResponse>> GetAllAsync();

    /// <summary>
    /// Alunos da escola: os que pertencem a ela, com o status atual, e os que saíram dela transferidos
    /// para outra escola da rede, como TRANSFERIDO. Nulo se a escola não existir ou estiver fora do escopo.
    /// </summary>
    Task<IReadOnlyList<AlunoResponse>?> GetByEscolaAsync(int escolaId);
    Task<AlunoCommandResult> CreateAsync(AlunoRequest request);
    Task<AlunoCommandResult> UpdateAsync(int id, AlunoRequest request);
    Task<AlunoCommandResult> UpdateStatusAsync(int id, AlunoStatusRequest request);
    Task<AlunoCommandResult> DeleteAsync(int id);
}
