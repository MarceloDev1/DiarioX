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

    public async Task<AnoLetivo?> GetVigenteAsync(DateOnly data)
    {
        return await _context.AnosLetivos
            .AsNoTracking()
            .Where(a => a.DataInicio <= data && a.DataTermino >= data)
            .OrderByDescending(a => a.DataInicio)
            .FirstOrDefaultAsync();
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

        // Os períodos são recriados; o encerramento (RF017 EX02) acompanha o número do período.
        var anteriores = existing.Periodos.ToDictionary(p => p.Numero);
        _context.PeriodosAvaliativos.RemoveRange(existing.Periodos);

        foreach (var periodo in entity.Periodos)
        {
            anteriores.TryGetValue(periodo.Numero, out var anterior);
            existing.Periodos.Add(new PeriodoAvaliativo
            {
                AnoLetivoId = existing.Id,
                Nome = periodo.Nome,
                Numero = periodo.Numero,
                DataInicio = periodo.DataInicio,
                DataTermino = periodo.DataTermino,
                Encerrado = anterior?.Encerrado ?? false,
                EncerradoEm = anterior?.EncerradoEm,
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DefinirPeriodoEncerradoAsync(PeriodoAvaliativo periodo, bool encerrado)
    {
        var atual = await _context.PeriodosAvaliativos.FirstAsync(p => p.Id == periodo.Id);
        atual.Encerrado = encerrado;
        atual.EncerradoEm = encerrado ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(AnoLetivo entity)
    {
        _context.AnosLetivos.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
