using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class ChamadaRepository : IChamadaRepository
{
    private readonly AppDbContext _context;

    public ChamadaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Chamada?> GetByIdAsync(int id)
        => Query().FirstOrDefaultAsync(c => c.Id == id);

    public Task<Chamada?> GetAsync(int turmaId, int disciplinaId, DateOnly data)
        => Query().FirstOrDefaultAsync(c => c.TurmaId == turmaId && c.DisciplinaId == disciplinaId && c.Data == data);

    public async Task<IReadOnlyList<Chamada>> ListAsync(int turmaId, int disciplinaId, DateOnly? de = null, DateOnly? ate = null)
    {
        var query = Query().Where(c => c.TurmaId == turmaId && c.DisciplinaId == disciplinaId);
        if (de is not null)
            query = query.Where(c => c.Data >= de);
        if (ate is not null)
            query = query.Where(c => c.Data <= ate);

        return await query.OrderByDescending(c => c.Data).ToListAsync();
    }

    public async Task<IReadOnlyList<AlunoTurma>> GetEnturmacoesAsync(int turmaId, DateOnly de, DateOnly ate)
    {
        return await _context.AlunosTurmas
            .AsNoTracking()
            .Include(at => at.Aluno)
            .Where(at => at.TurmaId == turmaId && at.DataInicio <= ate && (at.DataFim == null || at.DataFim >= de))
            .ToListAsync();
    }

    public async Task<IReadOnlyDictionary<int, string>> GetEmailsUsuariosAsync(IEnumerable<int> usuarioIds)
    {
        var ids = usuarioIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        // O Administrador global não pertence à instituição: o filtro de tenant o esconderia.
        return await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);
    }

    public async Task<Chamada> AddAsync(Chamada chamada)
    {
        _context.Chamadas.Add(chamada);
        await _context.SaveChangesAsync();
        return chamada;
    }

    public async Task UpdateAsync(Chamada chamada, IEnumerable<ChamadaAluno> registros)
    {
        var atual = await _context.Chamadas
            .Include(c => c.Registros)
            .FirstAsync(c => c.Id == chamada.Id);

        atual.QuantidadeAulas = chamada.QuantidadeAulas;
        atual.Conteudo = chamada.Conteudo;
        atual.AtualizadoPorUsuarioId = chamada.AtualizadoPorUsuarioId;
        atual.UpdatedAt = chamada.UpdatedAt;

        var novos = registros.ToDictionary(r => r.AlunoId);
        foreach (var registro in atual.Registros.ToList())
        {
            if (novos.Remove(registro.AlunoId, out var novo))
            {
                registro.Situacao = novo.Situacao;
                registro.Justificativa = novo.Justificativa;
            }
            else
            {
                atual.Registros.Remove(registro);
            }
        }

        foreach (var novo in novos.Values)
            atual.Registros.Add(novo);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Chamada chamada)
    {
        var atual = await _context.Chamadas.FirstAsync(c => c.Id == chamada.Id);
        _context.Chamadas.Remove(atual);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Chamada> Query()
        => _context.Chamadas
            .AsNoTracking()
            .Include(c => c.Turma).ThenInclude(t => t.AnoLetivo)
            .Include(c => c.Disciplina)
            .Include(c => c.Registros).ThenInclude(r => r.Aluno);
}
