using DiarioX.Server.Application.DTOs.Conteudos;

namespace DiarioX.Server.Application.Interfaces;

public interface IHabilidadeBnccService
{
    Task<IEnumerable<HabilidadeBnccResponse>> GetAllAsync();
    Task<HabilidadeBnccResponse?> GetByIdAsync(int id);
    Task<HabilidadeBnccCommandResult> CreateAsync(HabilidadeBnccRequest request);
    Task<HabilidadeBnccCommandResult> UpdateAsync(int id, HabilidadeBnccRequest request);
    Task<HabilidadeBnccCommandResult> DeleteAsync(int id);
}
