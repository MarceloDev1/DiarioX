using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class EtapaEnsinoRepository : BaseRepository<EtapaEnsino>, IEtapaEnsinoRepository
{
    public EtapaEnsinoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<EtapaEnsino?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.ModalidadeEnsino)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public override async Task<IEnumerable<EtapaEnsino>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.ModalidadeEnsino)
            .OrderBy(e => e.ModalidadeEnsino.Nome)
            .ThenBy(e => e.OrdemCronologica)
            .ToListAsync();
    }

    public async Task<bool> ExistsByNomeInModalidadeAsync(string nome, int modalidadeEnsinoId, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(e =>
            EF.Functions.ILike(e.Nome, nome) &&
            e.ModalidadeEnsinoId == modalidadeEnsinoId &&
            (!excludeId.HasValue || e.Id != excludeId.Value));
    }

    public async Task<bool> ExistsBySiglaAsync(string sigla, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(e =>
            EF.Functions.ILike(e.Sigla, sigla) &&
            (!excludeId.HasValue || e.Id != excludeId.Value));
    }

    public async Task<bool> ExistsByOrdemInModalidadeAsync(int ordem, int modalidadeEnsinoId, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(e =>
            e.OrdemCronologica == ordem &&
            e.ModalidadeEnsinoId == modalidadeEnsinoId &&
            (!excludeId.HasValue || e.Id != excludeId.Value));
    }

    public async Task DeleteAsync(int id)
    {
        var etapa = await _dbSet.FindAsync(id);
        if (etapa is not null)
            await DeleteAsync(etapa);
    }
}
