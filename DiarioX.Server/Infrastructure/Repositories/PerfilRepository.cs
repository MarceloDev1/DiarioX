using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class PerfilRepository : BaseRepository<Perfil>, IPerfilRepository
{
    public PerfilRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<Perfil>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<Perfil?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
