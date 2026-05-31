using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IEtapaEnsinoRepository
{
    Task<EtapaEnsino?> GetByIdAsync(int id);
    Task<IEnumerable<EtapaEnsino>> GetAllAsync();
    Task<bool> ExistsByNomeInModalidadeAsync(string nome, int modalidadeEnsinoId, int? excludeId = null);
    Task<bool> ExistsBySiglaAsync(string sigla, int? excludeId = null);
    Task<bool> ExistsByOrdemInModalidadeAsync(int ordem, int modalidadeEnsinoId, int? excludeId = null);
    Task<EtapaEnsino> AddAsync(EtapaEnsino etapa);
    Task UpdateAsync(EtapaEnsino etapa);
    Task DeleteAsync(int id);
}
