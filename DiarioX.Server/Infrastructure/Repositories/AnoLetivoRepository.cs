using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class AnoLetivoRepository : IAnoLetivoRepository
{
    private readonly AppDbContext _context;

    public AnoLetivoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AnoLetivo>> GetAllAsync()
    {
        return await _context.AnosLetivos
            .AsNoTracking()
            .Include(a => a.Periodos.OrderBy(p => p.Numero))
            .OrderByDescending(a => a.AnoReferencia)
            .ToListAsync();
    }

    public async Task<AnoLetivo?> GetByIdAsync(int id)
    {
        return await _context.AnosLetivos
            .AsNoTracking()
            .Include(a => a.Periodos.OrderBy(p => p.Numero))
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> ExistsByAnoReferenciaAsync(int anoReferencia, int? excludeId = null)
    {
        return await _context.AnosLetivos.AnyAsync(a =>
            a.AnoReferencia == anoReferencia &&
            (!excludeId.HasValue || a.Id != excludeId.Value));
    }

    public async Task<AnoLetivo> AddAsync(AnoLetivo entity)
    {
        await _context.AnosLetivos.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(AnoLetivo entity)
    {
        var existing = await _context.AnosLetivos
            .Include(a => a.Periodos)
            .FirstOrDefaultAsync(a => a.Id == entity.Id);

        if (existing is null) return;

        existing.AnoReferencia = entity.AnoReferencia;
        existing.DataInicio = entity.DataInicio;
        existing.DataTermino = entity.DataTermino;
        existing.TipoPeriodo = entity.TipoPeriodo;

        // Casa os períodos pelo número para preservar os Ids: as avaliações apontam para eles.
        var novos = entity.Periodos.ToDictionary(p => p.Numero);
        foreach (var periodo in existing.Periodos.ToList())
        {
            if (novos.Remove(periodo.Numero, out var novo))
            {
                periodo.Nome = novo.Nome;
                periodo.DataInicio = novo.DataInicio;
                periodo.DataTermino = novo.DataTermino;
            }
            else
            {
                _context.PeriodosAvaliativos.Remove(periodo);
            }
        }

        foreach (var periodo in novos.Values)
        {
            existing.Periodos.Add(new PeriodoAvaliativo
            {
                AnoLetivoId = existing.Id,
                Nome = periodo.Nome,
                Numero = periodo.Numero,
                DataInicio = periodo.DataInicio,
                DataTermino = periodo.DataTermino,
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> PeriodosPossuemAvaliacoesAsync(IEnumerable<int> periodoIds)
    {
        var ids = periodoIds.ToList();
        if (ids.Count == 0)
            return false;

        // O ano letivo é da rede toda: considera as avaliações de todas as escolas.
        return await _context.Avaliacoes
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .AnyAsync(a => ids.Contains(a.PeriodoAvaliativoId));
    }

    public async Task DeleteAsync(AnoLetivo entity)
    {
        _context.AnosLetivos.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
