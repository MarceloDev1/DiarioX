using DiarioX.Server.Application.DTOs.AnosLetivos;

namespace DiarioX.Server.Application.Interfaces;

public interface IAnoLetivoService
{
    Task<IEnumerable<AnoLetivoResponse>> GetAllAsync();
    Task<AnoLetivoResponse?> GetByIdAsync(int id);
    Task<AnoLetivoCommandResult> CreateAsync(AnoLetivoRequest request);
    Task<AnoLetivoCommandResult> UpdateAsync(int id, AnoLetivoRequest request);
    Task<AnoLetivoCommandResult> DeleteAsync(int id);

    /// <summary>RF017 EX02: encerra (ou reabre) o período avaliativo; encerrado, o diário não aceita mais alterações nele.</summary>
    Task<AnoLetivoCommandResult> DefinirPeriodoEncerradoAsync(int anoLetivoId, int periodoId, bool encerrado);
}
