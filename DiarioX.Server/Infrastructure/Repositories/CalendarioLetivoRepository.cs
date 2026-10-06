using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class CalendarioLetivoRepository : ICalendarioLetivoRepository
{
    private readonly AppDbContext _context;

    public CalendarioLetivoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<CalendarioLetivo?> GetAsync(int anoLetivoId, int? escolaId)
    {
        return _context.CalendariosLetivos
            .AsNoTracking()
            .Include(c => c.Eventos.OrderBy(e => e.Data))
            .FirstOrDefaultAsync(c => c.AnoLetivoId == anoLetivoId && c.EscolaId == escolaId);
    }

    public async Task<IReadOnlyList<CalendarioLetivo>> GetPublicadosAsync(int anoLetivoId, int escolaId)
    {
        return await _context.CalendariosLetivos
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .AsNoTracking()
            .Include(c => c.Eventos)
            .Where(c => c.AnoLetivoId == anoLetivoId && c.PublicadoEm != null && (c.EscolaId == null || c.EscolaId == escolaId))
            .ToListAsync();
    }

    public async Task<CalendarioLetivo> GetOrCreateAsync(int anoLetivoId, int? escolaId)
    {
        var existente = await _context.CalendariosLetivos
            .FirstOrDefaultAsync(c => c.AnoLetivoId == anoLetivoId && c.EscolaId == escolaId);
        if (existente is not null)
            return existente;

        var calendario = new CalendarioLetivo { AnoLetivoId = anoLetivoId, EscolaId = escolaId };
        _context.CalendariosLetivos.Add(calendario);
        try
        {
            await _context.SaveChangesAsync();
            return calendario;
        }
        catch (DbUpdateException)
        {
            // Outra requisição criou o mesmo calendário ao mesmo tempo (índice único): usa o dela.
            _context.Entry(calendario).State = EntityState.Detached;
            return await _context.CalendariosLetivos
                .FirstAsync(c => c.AnoLetivoId == anoLetivoId && c.EscolaId == escolaId);
        }
    }

    public async Task SubstituirEventosAsync(int calendarioId, DateOnly de, DateOnly ate, IEnumerable<EventoCalendario> eventos)
    {
        await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;

        var calendario = await _context.CalendariosLetivos.FirstAsync(c => c.Id == calendarioId);
        var anteriores = await _context.EventosCalendario
            .Where(e => e.CalendarioLetivoId == calendarioId && e.Data >= de && e.Data <= ate)
            .ToListAsync();

        // Remove antes de incluir: o índice único (calendário, data) não aceita os dois ao mesmo tempo.
        _context.EventosCalendario.RemoveRange(anteriores);
        await _context.SaveChangesAsync();

        foreach (var evento in eventos)
        {
            evento.CalendarioLetivoId = calendarioId;
            _context.EventosCalendario.Add(evento);
        }
        calendario.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async Task PublicarAsync(int calendarioId, int usuarioId)
    {
        var calendario = await _context.CalendariosLetivos.FirstAsync(c => c.Id == calendarioId);
        calendario.PublicadoEm = DateTime.UtcNow;
        calendario.PublicadoPorUsuarioId = usuarioId;
        await _context.SaveChangesAsync();
    }
}
