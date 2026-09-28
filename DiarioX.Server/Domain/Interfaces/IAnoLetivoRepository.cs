using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAnoLetivoRepository
{
    Task<IEnumerable<AnoLetivo>> GetAllAsync();
    Task<AnoLetivo?> GetByIdAsync(int id);
    Task<bool> ExistsByAnoReferenciaAsync(int anoReferencia, int? excludeId = null);
    Task<AnoLetivo> AddAsync(AnoLetivo entity);

    /// <summary>
    /// Atualiza o ano e os períodos casando-os pelo número: os existentes mantêm o Id (avaliações
    /// continuam vinculadas), os novos são incluídos e os que sobrarem são removidos.
    /// </summary>
    Task UpdateAsync(AnoLetivo entity);
    Task DeleteAsync(AnoLetivo entity);

    /// <summary>Indica se algum dos períodos tem avaliações (em qualquer escola da instituição).</summary>
    Task<bool> PeriodosPossuemAvaliacoesAsync(IEnumerable<int> periodoIds);
}
