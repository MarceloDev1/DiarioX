using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IHabilidadeBnccRepository
{
    Task<HabilidadeBncc?> GetByIdAsync(int id);
    Task<IReadOnlyList<HabilidadeBncc>> GetAllAsync();
    Task<IReadOnlyList<HabilidadeBncc>> GetByIdsAsync(IEnumerable<int> ids);

    /// <summary>RN02: habilidades ativas da disciplina que valem para a etapa de ensino, opcionalmente filtradas por código/descrição.</summary>
    Task<IReadOnlyList<HabilidadeBncc>> GetSugestoesAsync(int etapaEnsinoId, int disciplinaId, string? busca);

    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId = null);

    /// <summary>A habilidade já foi usada em algum conteúdo ministrado.</summary>
    Task<bool> EmUsoAsync(int id);

    Task<HabilidadeBncc> AddAsync(HabilidadeBncc habilidade);
    Task UpdateAsync(HabilidadeBncc habilidade);
    Task DeleteAsync(int id);
}
