using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class ConteudoMinistradoRepository : IConteudoMinistradoRepository
{
    private readonly AppDbContext _context;

    public ConteudoMinistradoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ConteudoMinistrado?> GetByIdAsync(int id)
        => Query().FirstOrDefaultAsync(c => c.Id == id);

    public Task<ConteudoMinistrado?> GetAsync(int turmaId, int disciplinaId, DateOnly data)
        => Query().FirstOrDefaultAsync(c => c.TurmaId == turmaId && c.DisciplinaId == disciplinaId && c.Data == data);

    public async Task<IReadOnlyList<ConteudoMinistrado>> ListAsync(int turmaId, int? disciplinaId, DateOnly? de = null, DateOnly? ate = null)
    {
        var query = Query().Where(c => c.TurmaId == turmaId);
        if (disciplinaId is not null)
            query = query.Where(c => c.DisciplinaId == disciplinaId);
        if (de is not null)
            query = query.Where(c => c.Data >= de);
        if (ate is not null)
            query = query.Where(c => c.Data <= ate);

        return await query.OrderByDescending(c => c.Data).ThenBy(c => c.Disciplina.Nome).ToListAsync();
    }

    public async Task<ConteudoMinistrado> AddAsync(ConteudoMinistrado conteudo)
    {
        _context.ConteudosMinistrados.Add(conteudo);
        await _context.SaveChangesAsync();
        return conteudo;
    }

    public async Task UpdateAsync(ConteudoMinistrado conteudo, IEnumerable<int> habilidadeIds)
    {
        var atual = await _context.ConteudosMinistrados
            .Include(c => c.Habilidades)
            .FirstAsync(c => c.Id == conteudo.Id);

        atual.Descricao = conteudo.Descricao;
        atual.AtualizadoPorUsuarioId = conteudo.AtualizadoPorUsuarioId;
        atual.UpdatedAt = conteudo.UpdatedAt;

        var novas = habilidadeIds.ToHashSet();
        foreach (var vinculo in atual.Habilidades.Where(h => !novas.Contains(h.HabilidadeBnccId)).ToList())
            atual.Habilidades.Remove(vinculo);

        var existentes = atual.Habilidades.Select(h => h.HabilidadeBnccId).ToHashSet();
        foreach (var id in novas.Where(id => !existentes.Contains(id)))
            atual.Habilidades.Add(new ConteudoMinistradoHabilidade { HabilidadeBnccId = id });

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var atual = await _context.ConteudosMinistrados.FirstOrDefaultAsync(c => c.Id == id);
        if (atual is null)
            return;

        _context.ConteudosMinistrados.Remove(atual);
        await _context.SaveChangesAsync();
    }

    private IQueryable<ConteudoMinistrado> Query()
        => _context.ConteudosMinistrados
            .AsNoTracking()
            .Include(c => c.Turma).ThenInclude(t => t.AnoLetivo)
            .Include(c => c.Disciplina)
            .Include(c => c.Habilidades).ThenInclude(h => h.HabilidadeBncc);
}
