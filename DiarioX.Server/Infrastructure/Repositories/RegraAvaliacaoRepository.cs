using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class RegraAvaliacaoRepository : IRegraAvaliacaoRepository
{
    private readonly AppDbContext _context;

    public RegraAvaliacaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RegraAvaliacao>> GetAllAsync()
        => await Query().OrderBy(r => r.Nome).ToListAsync();

    public Task<RegraAvaliacao?> GetByIdAsync(int id)
        => Query().FirstOrDefaultAsync(r => r.Id == id);

    public Task<RegraAvaliacao?> GetByEtapaAsync(int etapaEnsinoId)
        => _context.RegrasAvaliacao
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Etapas.Any(e => e.Id == etapaEnsinoId));

    public Task<bool> ExistsByNomeAsync(string nome, int? excludeId = null)
        => _context.RegrasAvaliacao.AnyAsync(r =>
            r.Nome.ToLower() == nome.ToLower() && (!excludeId.HasValue || r.Id != excludeId.Value));

    public async Task<RegraAvaliacao> AddAsync(RegraAvaliacao regra, IReadOnlyCollection<int> etapaIds)
    {
        _context.RegrasAvaliacao.Add(regra);
        foreach (var etapa in await _context.EtapasEnsino.Where(e => etapaIds.Contains(e.Id)).ToListAsync())
            etapa.RegraAvaliacao = regra;

        await _context.SaveChangesAsync();
        return regra;
    }

    public async Task UpdateAsync(RegraAvaliacao regra, IReadOnlyCollection<int> etapaIds)
    {
        var atual = await _context.RegrasAvaliacao.FirstAsync(r => r.Id == regra.Id);
        atual.Nome = regra.Nome;
        atual.NotaMaxima = regra.NotaMaxima;
        atual.MediaAprovacao = regra.MediaAprovacao;
        atual.CasasDecimais = regra.CasasDecimais;
        atual.CalculoNotaPeriodo = regra.CalculoNotaPeriodo;
        atual.PermiteRecuperacao = regra.PermiteRecuperacao;
        atual.UpdatedAt = regra.UpdatedAt;

        // Etapas marcadas passam para esta regra (mesmo que estivessem em outra); as desmarcadas voltam ao padrão.
        var etapas = await _context.EtapasEnsino
            .Where(e => e.RegraAvaliacaoId == regra.Id || etapaIds.Contains(e.Id))
            .ToListAsync();
        foreach (var etapa in etapas)
            etapa.RegraAvaliacaoId = etapaIds.Contains(etapa.Id) ? regra.Id : null;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var regra = await _context.RegrasAvaliacao.Include(r => r.Etapas).FirstAsync(r => r.Id == id);
        foreach (var etapa in regra.Etapas)
            etapa.RegraAvaliacaoId = null;

        _context.RegrasAvaliacao.Remove(regra);
        await _context.SaveChangesAsync();
    }

    private IQueryable<RegraAvaliacao> Query()
        => _context.RegrasAvaliacao
            .AsNoTracking()
            .Include(r => r.Etapas.OrderBy(e => e.OrdemCronologica))
            .ThenInclude(e => e.ModalidadeEnsino);
}
