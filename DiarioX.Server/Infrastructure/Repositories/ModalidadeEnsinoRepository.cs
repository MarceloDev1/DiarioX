using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class ModalidadeEnsinoRepository : BaseRepository<ModalidadeEnsino>, IModalidadeEnsinoRepository
{
    public ModalidadeEnsinoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ModalidadeEnsino?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public override async Task<IEnumerable<ModalidadeEnsino>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(m => m.Nome)
            .ToListAsync();
    }

    public async Task<bool> ExistsByNomeAsync(string nome, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(m =>
            EF.Functions.ILike(m.Nome, nome) &&
            (!excludeId.HasValue || m.Id != excludeId.Value));
    }

    public async Task<bool> ExistsBySiglaAsync(string sigla, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(m =>
            EF.Functions.ILike(m.Sigla, sigla) &&
            (!excludeId.HasValue || m.Id != excludeId.Value));
    }

    public async Task DeleteAsync(int id)
    {
        var modalidade = await _dbSet.FindAsync(id);
        if (modalidade is not null)
            await DeleteAsync(modalidade);
    }
}
