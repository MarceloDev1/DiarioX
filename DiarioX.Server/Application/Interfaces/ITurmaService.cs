using DiarioX.Server.Application.DTOs.Turmas;

namespace DiarioX.Server.Application.Interfaces;

public interface ITurmaService
{
    Task<IEnumerable<TurmaResponse>> GetAllAsync();
    Task<TurmaResponse?> GetByIdAsync(int id);
    Task<TurmaCommandResult> CreateAsync(TurmaRequest request);
}