using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class HabilidadeBnccRepository : IHabilidadeBnccRepository
{
    private readonly AppDbContext _context;

    public HabilidadeBnccRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<HabilidadeBncc?> GetByIdAsync(int id)
        => Query().FirstOrDefaultAsync(h => h.Id == id);

    public async Task<IReadOnlyList<HabilidadeBncc>> GetAllAsync()
        => await Query().OrderBy(h => h.Codigo).ToListAsync();

    public async Task<IReadOnlyList<HabilidadeBncc>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        return await Query().Where(h => lista.Contains(h.Id)).ToListAsync();
    }

    public async Task<IReadOnlyList<HabilidadeBncc>> GetSugestoesAsync(int etapaEnsinoId, int disciplinaId, string? busca)
    {
        var query = Query().Where(h => h.Ativa && h.DisciplinaId == disciplinaId &&
                                       h.EtapasEnsino.Any(e => e.EtapaEnsinoId == etapaEnsinoId));

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(h => h.Codigo.ToLower().Contains(termo) || h.Descricao.ToLower().Contains(termo));
        }

        return await query.OrderBy(h => h.Codigo).ToListAsync();
    }

    public Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId = null)
        => _context.HabilidadesBncc.AnyAsync(h =>
            h.Codigo.ToUpper() == codigo.ToUpper() && (!excludeId.HasValue || h.Id != excludeId.Value));

    public Task<bool> EmUsoAsync(int id)
        => _context.ConteudosMinistradosHabilidades.AnyAsync(h => h.HabilidadeBnccId == id);

    public async Task<HabilidadeBncc> AddAsync(HabilidadeBncc habilidade)
    {
        _context.HabilidadesBncc.Add(habilidade);
        await _context.SaveChangesAsync();
        return (await GetByIdAsync(habilidade.Id))!;
    }

    public async Task UpdateAsync(HabilidadeBncc habilidade)
    {
        var atual = await _context.HabilidadesBncc
            .Include(h => h.EtapasEnsino)
            .FirstOrDefaultAsync(h => h.Id == habilidade.Id);
        if (atual is null)
            return;

        atual.Codigo = habilidade.Codigo;
        atual.Descricao = habilidade.Descricao;
        atual.DisciplinaId = habilidade.DisciplinaId;
        atual.Ativa = habilidade.Ativa;

        var novas = habilidade.EtapasEnsino.Select(e => e.EtapaEnsinoId).ToHashSet();
        foreach (var etapa in atual.EtapasEnsino.Where(e => !novas.Contains(e.EtapaEnsinoId)).ToList())
            atual.EtapasEnsino.Remove(etapa);

        var existentes = atual.EtapasEnsino.Select(e => e.EtapaEnsinoId).ToHashSet();
        foreach (var etapaId in novas.Where(id => !existentes.Contains(id)))
            atual.EtapasEnsino.Add(new HabilidadeBnccEtapaEnsino { EtapaEnsinoId = etapaId });

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var atual = await _context.HabilidadesBncc.FirstOrDefaultAsync(h => h.Id == id);
        if (atual is null)
            return;

        _context.HabilidadesBncc.Remove(atual);
        await _context.SaveChangesAsync();
    }

    private IQueryable<HabilidadeBncc> Query()
        => _context.HabilidadesBncc
            .AsNoTracking()
            .Include(h => h.Disciplina)
            .Include(h => h.EtapasEnsino).ThenInclude(e => e.EtapaEnsino);
}
