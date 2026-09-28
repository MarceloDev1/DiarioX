using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IRegraAvaliacaoRepository
{
    /// <summary>Regras com as etapas vinculadas.</summary>
    Task<IReadOnlyList<RegraAvaliacao>> GetAllAsync();
    Task<RegraAvaliacao?> GetByIdAsync(int id);

    /// <summary>Regra vinculada à etapa; nula quando a etapa usa a regra padrão do sistema.</summary>
    Task<RegraAvaliacao?> GetByEtapaAsync(int etapaEnsinoId);

    Task<bool> ExistsByNomeAsync(string nome, int? excludeId = null);

    /// <summary>Grava a regra e passa a vinculá-la exatamente às etapas informadas.</summary>
    Task<RegraAvaliacao> AddAsync(RegraAvaliacao regra, IReadOnlyCollection<int> etapaIds);
    Task UpdateAsync(RegraAvaliacao regra, IReadOnlyCollection<int> etapaIds);

    /// <summary>Remove a regra; as etapas vinculadas voltam para a regra padrão do sistema.</summary>
    Task DeleteAsync(int id);
}
