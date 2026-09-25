using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(int id);
    Task<Tenant?> GetBySlugAsync(string slug);
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null);
    Task<Tenant> AddAsync(Tenant tenant);
    Task UpdateAsync(Tenant tenant);
}
