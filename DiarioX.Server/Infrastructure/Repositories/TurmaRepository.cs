using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class TurmaRepository : BaseRepository<Turma>, ITurmaRepository
{
    public TurmaRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Turma?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.AnoLetivo)
            .Include(t => t.Escola)
            .Include(t => t.ModalidadeEnsino)
            .Include(t => t.EtapaEnsino)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public override async Task<IEnumerable<Turma>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.AnoLetivo)
            .Include(t => t.Escola)
            .Include(t => t.ModalidadeEnsino)
            .Include(t => t.EtapaEnsino)
            .OrderByDescending(t => t.AnoLetivo.AnoReferencia)
            .ThenBy(t => t.Escola.Nome)
            .ThenBy(t => t.NomeCompleto)
            .ToListAsync();
    }

    public async Task<bool> ExistsByCombinacaoAsync(
        int anoLetivoId,
        int escolaId,
        int etapaEnsinoId,
        string nomeIdentificador,
        string turno,
        int? excludeId = null)
    {
        return await _dbSet.AnyAsync(t =>
            t.AnoLetivoId == anoLetivoId &&
            t.EscolaId == escolaId &&
            t.EtapaEnsinoId == etapaEnsinoId &&
            EF.Functions.ILike(t.NomeIdentificador, nomeIdentificador) &&
            t.Turno == turno &&
            (!excludeId.HasValue || t.Id != excludeId.Value));
    }
}