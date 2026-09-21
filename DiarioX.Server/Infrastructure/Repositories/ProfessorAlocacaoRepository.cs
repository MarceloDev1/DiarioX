using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class ProfessorAlocacaoRepository : BaseRepository<ProfessorAlocacao>, IProfessorAlocacaoRepository
{
    public ProfessorAlocacaoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ProfessorAlocacao?> GetByIdAsync(int id)
        => await Query().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IEnumerable<ProfessorAlocacao>> GetByProfessorIdAsync(int professorId)
        => await Query().Where(x => x.ProfessorId == professorId && x.Ativa).ToListAsync();

    public async Task<IEnumerable<ProfessorAlocacao>> GetAtivasAsync()
        => await Query().Where(x => x.Ativa).ToListAsync();

    public async Task SaveAsync(IEnumerable<ProfessorAlocacao> novas, IEnumerable<int> removerIds)
    {
        var ids = removerIds.Distinct().ToList();
        var existentes = await _context.ProfessorAlocacoes
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();

        _context.ProfessorAlocacoes.RemoveRange(existentes);
        await _context.ProfessorAlocacoes.AddRangeAsync(novas);
        await _context.SaveChangesAsync();
    }

    public override async Task DeleteAsync(ProfessorAlocacao alocacao)
    {
        _context.ProfessorAlocacoes.Remove(alocacao);
        await _context.SaveChangesAsync();
    }

    private IQueryable<ProfessorAlocacao> Query()
        => _context.ProfessorAlocacoes
            .AsNoTracking()
            .Include(x => x.Professor)
            .Include(x => x.Turma)
                .ThenInclude(x => x.AnoLetivo)
            .Include(x => x.Disciplina);
}