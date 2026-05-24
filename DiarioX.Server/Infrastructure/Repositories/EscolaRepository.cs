using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class EscolaRepository : BaseRepository<Escola>, IEscolaRepository
{
    public EscolaRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Escola?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public override async Task<IEnumerable<Escola>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(e => e.Nome)
            .ToListAsync();
    }

    public async Task<bool> ExistsByCodigoInepAsync(string codigoInep, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(e =>
            EF.Functions.ILike(e.CodigoInep, codigoInep) &&
            (!excludeId.HasValue || e.Id != excludeId.Value));
    }

    public async Task DeleteAsync(int id)
    {
        var escola = await _dbSet.FindAsync(id);
        if (escola is not null)
        {
            await DeleteAsync(escola);
        }
    }
}
