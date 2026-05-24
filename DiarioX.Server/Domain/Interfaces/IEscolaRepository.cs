using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IEscolaRepository
{
    Task<Escola?> GetByIdAsync(int id);
    Task<IEnumerable<Escola>> GetAllAsync();
    Task<bool> ExistsByCodigoInepAsync(string codigoInep, int? excludeId = null);
    Task<Escola> AddAsync(Escola escola);
    Task UpdateAsync(Escola escola);
    Task DeleteAsync(int id);
}
