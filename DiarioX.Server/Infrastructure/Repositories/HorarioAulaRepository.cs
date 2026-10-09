using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class HorarioAulaRepository : IHorarioAulaRepository
{
    private readonly AppDbContext _context;

    public HorarioAulaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HorarioAula>> ListByTurmaAsync(int turmaId)
        => await _context.HorariosAula
            .AsNoTracking()
            .Include(h => h.Disciplina)
            .Where(h => h.TurmaId == turmaId)
            .OrderBy(h => h.DiaSemana).ThenBy(h => h.Ordem)
            .ToListAsync();

    public async Task<IReadOnlyList<HorarioAula>> ListByTurmasAsync(IReadOnlyCollection<int> turmaIds)
        => await _context.HorariosAula
            .AsNoTracking()
            .Where(h => turmaIds.Contains(h.TurmaId))
            .ToListAsync();

    public async Task SubstituirAsync(int turmaId, IReadOnlyCollection<HorarioAula> tempos)
    {
        var atuais = await _context.HorariosAula.Where(h => h.TurmaId == turmaId).ToListAsync();
        var novos = tempos.ToDictionary(t => (t.DiaSemana, t.Ordem));

        // Atualiza no lugar os tempos que continuam ocupados, para não esbarrar no índice único.
        foreach (var atual in atuais)
        {
            if (novos.Remove((atual.DiaSemana, atual.Ordem), out var novo))
                atual.DisciplinaId = novo.DisciplinaId;
            else
                _context.HorariosAula.Remove(atual);
        }

        foreach (var novo in novos.Values)
        {
            _context.HorariosAula.Add(new HorarioAula
            {
                TurmaId = turmaId, DiaSemana = novo.DiaSemana, Ordem = novo.Ordem, DisciplinaId = novo.DisciplinaId,
            });
        }

        await _context.SaveChangesAsync();
    }
}
