using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class AvaliacaoRepository : IAvaliacaoRepository
{
    private readonly AppDbContext _context;

    public AvaliacaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Avaliacao?> GetByIdAsync(int id)
        => Query().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<Avaliacao>> ListAsync(int turmaId, int disciplinaId, int? periodoId = null)
    {
        var query = Query().Where(a => a.TurmaId == turmaId && a.DisciplinaId == disciplinaId);
        if (periodoId is not null)
            query = query.Where(a => a.PeriodoAvaliativoId == periodoId);

        return await query.OrderBy(a => a.Data == null).ThenBy(a => a.Data).ThenBy(a => a.Id).ToListAsync();
    }

    public async Task<IReadOnlyList<Avaliacao>> ListByTurmaAsync(int turmaId)
        => await Query().Where(a => a.TurmaId == turmaId).ToListAsync();

    public async Task<IReadOnlyList<AlunoTurma>> GetEnturmacoesAsync(int turmaId, DateOnly de, DateOnly ate)
    {
        return await _context.AlunosTurmas
            .AsNoTracking()
            .Include(at => at.Aluno)
            .Where(at => at.TurmaId == turmaId && at.DataInicio <= ate && (at.DataFim == null || at.DataFim >= de))
            .ToListAsync();
    }

    public async Task<Avaliacao> AddAsync(Avaliacao avaliacao)
    {
        _context.Avaliacoes.Add(avaliacao);
        await _context.SaveChangesAsync();
        return avaliacao;
    }

    public async Task UpdateAsync(Avaliacao avaliacao)
    {
        var atual = await _context.Avaliacoes.FirstAsync(a => a.Id == avaliacao.Id);
        atual.Nome = avaliacao.Nome;
        atual.Tipo = avaliacao.Tipo;
        atual.Data = avaliacao.Data;
        atual.Peso = avaliacao.Peso;
        atual.ValorMaximo = avaliacao.ValorMaximo;
        atual.AtualizadoPorUsuarioId = avaliacao.AtualizadoPorUsuarioId;
        atual.UpdatedAt = avaliacao.UpdatedAt;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var atual = await _context.Avaliacoes.Include(a => a.Notas).FirstAsync(a => a.Id == id);
        _context.NotasAvaliacoes.RemoveRange(atual.Notas);
        _context.Avaliacoes.Remove(atual);
        await _context.SaveChangesAsync();
    }

    public async Task SalvarNotasAsync(IReadOnlyCollection<NotaInformada> notas, int usuarioId)
    {
        if (notas.Count == 0)
            return;

        var avaliacaoIds = notas.Select(n => n.AvaliacaoId).Distinct().ToList();
        var existentes = (await _context.NotasAvaliacoes
                .Where(n => avaliacaoIds.Contains(n.AvaliacaoId))
                .ToListAsync())
            .ToDictionary(n => (n.AvaliacaoId, n.AlunoId));

        var agora = DateTime.UtcNow;
        foreach (var informada in notas)
        {
            existentes.TryGetValue((informada.AvaliacaoId, informada.AlunoId), out var atual);
            if (informada.Valor is not decimal valor)
            {
                if (atual is not null)
                    _context.NotasAvaliacoes.Remove(atual);
            }
            else if (atual is null)
            {
                _context.NotasAvaliacoes.Add(new NotaAvaliacao
                {
                    AvaliacaoId = informada.AvaliacaoId,
                    AlunoId = informada.AlunoId,
                    Valor = valor,
                    LancadaPorUsuarioId = usuarioId,
                    LancadaEm = agora,
                });
            }
            else if (atual.Valor != valor)
            {
                atual.Valor = valor;
                atual.LancadaPorUsuarioId = usuarioId;
                atual.LancadaEm = agora;
            }
        }

        await _context.SaveChangesAsync();
    }

    private IQueryable<Avaliacao> Query()
        => _context.Avaliacoes
            .AsNoTracking()
            .Include(a => a.Notas).ThenInclude(n => n.Aluno);
}
