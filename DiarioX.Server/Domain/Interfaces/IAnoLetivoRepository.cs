using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IAnoLetivoRepository
{
    Task<IEnumerable<AnoLetivo>> GetAllAsync();
    Task<AnoLetivo?> GetByIdAsync(int id);

    /// <summary>Ano letivo cujo período contém a data; nulo se nenhum contiver.</summary>
    Task<AnoLetivo?> GetVigenteAsync(DateOnly data);
    Task<bool> ExistsByAnoReferenciaAsync(int anoReferencia, int? excludeId = null);
    Task<AnoLetivo> AddAsync(AnoLetivo entity);
    Task UpdateAsync(AnoLetivo entity);
    Task DeleteAsync(AnoLetivo entity);
}
