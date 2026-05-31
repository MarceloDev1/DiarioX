using DiarioX.Server.Application.DTOs.AnosLetivos;

namespace DiarioX.Server.Application.Interfaces;

public interface IAnoLetivoService
{
    Task<IEnumerable<AnoLetivoResponse>> GetAllAsync();
    Task<AnoLetivoResponse?> GetByIdAsync(int id);
    Task<AnoLetivoCommandResult> CreateAsync(AnoLetivoRequest request);
    Task<AnoLetivoCommandResult> UpdateAsync(int id, AnoLetivoRequest request);
    Task<AnoLetivoCommandResult> DeleteAsync(int id);
}
