using DiarioX.Server.Application.DTOs.EtapasEnsino;

namespace DiarioX.Server.Application.Interfaces;

public interface IEtapaEnsinoService
{
    Task<IEnumerable<EtapaEnsinoResponse>> GetAllAsync();
    Task<EtapaEnsinoResponse?> GetByIdAsync(int id);
    Task<EtapaEnsinoCommandResult> CreateAsync(EtapaEnsinoRequest request);
    Task<EtapaEnsinoCommandResult> UpdateAsync(int id, EtapaEnsinoRequest request);
    Task<EtapaEnsinoCommandResult> DeleteAsync(int id);
}
