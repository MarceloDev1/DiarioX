using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
{
    public TenantRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);
    }

    public override async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(t =>
            t.Slug == slug &&
            (!excludeId.HasValue || t.Id != excludeId.Value));
    }
}
