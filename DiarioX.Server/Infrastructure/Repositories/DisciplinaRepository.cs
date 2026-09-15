using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class DisciplinaRepository : BaseRepository<Disciplina>, IDisciplinaRepository
{
    public DisciplinaRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Disciplina?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(d => d.EtapasEnsino)
            .ThenInclude(de => de.EtapaEnsino)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public override async Task<IEnumerable<Disciplina>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(d => d.EtapasEnsino)
            .ThenInclude(de => de.EtapaEnsino)
            .OrderBy(d => d.Nome)
            .ToListAsync();
    }

    public async Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(d =>
            EF.Functions.ILike(d.Codigo, codigo) &&
            (!excludeId.HasValue || d.Id != excludeId.Value));
    }

    public async Task<bool> ExistsByNomeAsync(string nome, int? excludeId = null)
    {
        return await _dbSet.AnyAsync(d =>
            EF.Functions.ILike(d.Nome, nome) &&
            (!excludeId.HasValue || d.Id != excludeId.Value));
    }

    public override async Task<Disciplina> AddAsync(Disciplina disciplina)
    {
        await _dbSet.AddAsync(disciplina);
        await _context.SaveChangesAsync();
        // Recarrega com relacionamentos
        return (await GetByIdAsync(disciplina.Id))!;
    }

    public override async Task UpdateAsync(Disciplina disciplina)
    {
        var existing = await _context.Disciplinas
            .Include(d => d.EtapasEnsino)
            .FirstOrDefaultAsync(d => d.Id == disciplina.Id);

        if (existing is null) return;

        existing.Nome = disciplina.Nome;
        existing.Codigo = disciplina.Codigo;
        existing.Descricao = disciplina.Descricao;
        existing.Ativa = disciplina.Ativa;

        // Remove etapas antigas e adiciona novas
        _context.DisciplinasEtapasEnsino.RemoveRange(existing.EtapasEnsino);
        
        foreach (var etapa in disciplina.EtapasEnsino)
        {
            existing.EtapasEnsino.Add(new DisciplinaEtapaEnsino
            {
                DisciplinaId = existing.Id,
                EtapaEnsinoId = etapa.EtapaEnsinoId
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var disciplina = await _dbSet.FindAsync(id);
        if (disciplina is not null)
        {
            _dbSet.Remove(disciplina);
            await _context.SaveChangesAsync();
        }
    }
}
