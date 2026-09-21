using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class AlunoRepository : BaseRepository<Aluno>, IAlunoRepository
{
    public AlunoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Aluno?> GetByIdAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Escola)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public override async Task<IEnumerable<Aluno>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Escola)
            .OrderBy(a => a.Nome)
            .ToListAsync();
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, int? excludeId = null)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(a => a.CpfAluno != null && a.CpfAluno == cpf && (excludeId == null || a.Id != excludeId));
    }

    public async Task<bool> ExistsByNomeDataNascimentoResponsavelAsync(string nome, DateTime dataNascimento, string responsavelNome1, int? excludeId = null)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(a =>
                EF.Functions.ILike(a.Nome, nome) &&
                a.DataNascimento == dataNascimento &&
                EF.Functions.ILike(a.ResponsavelNome1, responsavelNome1) &&
                (excludeId == null || a.Id != excludeId));
    }

    public async Task<bool> ExistsByMatriculaAsync(string matricula)
    {
        return await _dbSet.AsNoTracking().AnyAsync(a => a.Matricula == matricula);
    }

    public async Task<int> GetMaxSequencialMatriculaAsync(int ano)
    {
        var prefixo = ano.ToString();
        var matriculas = await _dbSet
            .AsNoTracking()
            .Where(a => a.Matricula.StartsWith(prefixo))
            .Select(a => a.Matricula)
            .ToListAsync();

        var maiorSequencial = 0;
        foreach (var matricula in matriculas)
        {
            var sufixo = matricula.Length > prefixo.Length ? matricula[prefixo.Length..] : string.Empty;
            if (int.TryParse(sufixo, out var sequencial) && sequencial > maiorSequencial)
                maiorSequencial = sequencial;
        }

        return maiorSequencial;
    }
}
