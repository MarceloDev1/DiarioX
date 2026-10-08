using DiarioX.Server.Application.DTOs.RegrasAvaliacao;

namespace DiarioX.Server.Application.Interfaces;

public interface IRegraAvaliacaoService
{
    Task<IEnumerable<RegraAvaliacaoResponse>> GetAllAsync();
    Task<RegraAvaliacaoResponse?> GetByIdAsync(int id);

    /// <summary>Regra usada pelas etapas sem regra própria.</summary>
    RegraAvaliacaoResponse GetPadrao();

    Task<RegraAvaliacaoCommandResult> CreateAsync(RegraAvaliacaoRequest request);
    Task<RegraAvaliacaoCommandResult> UpdateAsync(int id, RegraAvaliacaoRequest request);
    Task<RegraAvaliacaoCommandResult> DeleteAsync(int id);
}
