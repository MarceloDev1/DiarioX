using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class ProfessorRepository : BaseRepository<Professor>, IProfessorRepository
{
    public ProfessorRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Professor?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.ProfessorEscolas)
            .ThenInclude(pe => pe.Escola)
            .Include(p => p.ProfessorDisciplinas)
            .ThenInclude(pd => pd.Disciplina)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Vínculo com o usuário e unicidade valem para a instituição inteira, não só para as escolas do usuário.
    private IQueryable<Professor> DaInstituicao => _dbSet.IgnoreQueryFilters([AppDbContext.FiltroEscola]).AsNoTracking();

    public async Task<Professor?> GetByUsuarioIdAsync(int usuarioId)
    {
        return await DaInstituicao
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
    }

    public async Task<Professor?> GetByCpfAsync(string cpf)
    {
        return await DaInstituicao
            .FirstOrDefaultAsync(p =>
                EF.Functions.ILike(p.Cpf, cpf));
    }

    public async Task<Professor?> GetByMatriculaAsync(string matricula)
    {
        return await DaInstituicao
            .FirstOrDefaultAsync(p =>
                p.Matricula != null && EF.Functions.ILike(p.Matricula, matricula));
    }

    public override async Task<IEnumerable<Professor>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.ProfessorEscolas)
            .ThenInclude(pe => pe.Escola)
            .Include(p => p.ProfessorDisciplinas)
            .ThenInclude(pd => pd.Disciplina)
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task AddDisciplinaAsync(int professorId, int disciplinaId)
    {
        var profDisciplina = new ProfessorDisciplina
        {
            ProfessorId = professorId,
            DisciplinaId = disciplinaId,
            CreatedAt = DateTime.UtcNow,
        };

        await _context.ProfessorDisciplinas.AddAsync(profDisciplina);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveDisciplinasAsync(int professorId)
    {
        var disciplinas = await _context.ProfessorDisciplinas
            .Where(pd => pd.ProfessorId == professorId)
            .ToListAsync();

        _context.ProfessorDisciplinas.RemoveRange(disciplinas);
        await _context.SaveChangesAsync();
    }

    public async Task AddEscolaAsync(int professorId, int escolaId)
    {
        var profEscola = new ProfessorEscola
        {
            ProfessorId = professorId,
            EscolaId = escolaId,
            CreatedAt = DateTime.UtcNow,
        };

        await _context.ProfessorEscolas.AddAsync(profEscola);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveEscolasAsync(int professorId)
    {
        var escolas = await _context.ProfessorEscolas
            .Where(pe => pe.ProfessorId == professorId)
            .ToListAsync();

        _context.ProfessorEscolas.RemoveRange(escolas);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Professor>> GetByEscolaIdAsync(int escolaId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.ProfessorEscolas)
            .ThenInclude(pe => pe.Escola)
            .Include(p => p.ProfessorDisciplinas)
            .ThenInclude(pd => pd.Disciplina)
            .Where(p => p.ProfessorEscolas.Any(pe => pe.EscolaId == escolaId))
            .OrderBy(p => p.Nome)
            .ToListAsync();
    }

    public async Task<bool> ExistsByCpfAsync(string cpf)
    {
        return await DaInstituicao
            .AnyAsync(p => EF.Functions.ILike(p.Cpf, cpf));
    }

    public async Task<bool> ExistsByMatriculaAsync(string matricula)
    {
        return await DaInstituicao
            .AnyAsync(p => p.Matricula != null && EF.Functions.ILike(p.Matricula, matricula));
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await DaInstituicao
            .AnyAsync(p => EF.Functions.ILike(p.Email, email));
    }

    public override async Task DeleteAsync(Professor entity)
    {
        // Remove all professor disciplines first
        var disciplinas = await _context.ProfessorDisciplinas
            .Where(pd => pd.ProfessorId == entity.Id)
            .ToListAsync();

        _context.ProfessorDisciplinas.RemoveRange(disciplinas);

        // Inclui os vínculos com escolas fora do escopo do usuário, senão a exclusão esbarraria neles.
        var escolas = await _context.ProfessorEscolas
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .Where(pe => pe.ProfessorId == entity.Id)
            .ToListAsync();

        _context.ProfessorEscolas.RemoveRange(escolas);
        await _context.SaveChangesAsync();

        // Then remove professor
        await base.DeleteAsync(entity);
    }
}
