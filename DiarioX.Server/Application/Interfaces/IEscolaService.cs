using DiarioX.Server.Application.DTOs.Escolas;

namespace DiarioX.Server.Application.Interfaces;

public interface IEscolaService
{
    Task<IEnumerable<EscolaResponse>> GetAllAsync();
    Task<EscolaResponse?> GetByIdAsync(int id);
    Task<EscolaCommandResult> CreateAsync(EscolaRequest request);
    Task<EscolaCommandResult> UpdateAsync(int id, EscolaRequest request);
    Task<EscolaCommandResult> DeleteAsync(int id);
}
