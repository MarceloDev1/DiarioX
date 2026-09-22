using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class AlunoTurmaRepository : IAlunoTurmaRepository
{
    private readonly AppDbContext _context;

    public AlunoTurmaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AlunoTurma?> GetAtivaByAlunoIdAsync(int alunoId)
    {
        return _context.Set<AlunoTurma>()
            .Include(x => x.Aluno).ThenInclude(x => x.Escola)
            .Include(x => x.Turma).ThenInclude(x => x.AnoLetivo)
            .Include(x => x.Turma).ThenInclude(x => x.Escola)
            .FirstOrDefaultAsync(x => x.AlunoId == alunoId && x.DataFim == null);
    }

    public async Task<bool> HasVacancyAsync(int turmaId, DateOnly dataMovimentacao)
    {
        var turma = await _context.Turmas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == turmaId);
        if (turma is null)
            return false;

        var ocupacao = await _context.Set<AlunoTurma>().CountAsync(x =>
            x.TurmaId == turmaId &&
            x.DataInicio <= dataMovimentacao &&
            (x.DataFim == null || x.DataFim >= dataMovimentacao));

        return ocupacao < turma.VagasOfertadas;
    }

    public async Task EnturmarAsync(int alunoId, int turmaId, DateOnly dataInicio)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var aluno = await _context.Set<Aluno>().FirstAsync(x => x.Id == alunoId);
        var vinculoAtivo = await _context.Set<AlunoTurma>()
            .AnyAsync(x => x.AlunoId == alunoId && x.DataFim == null);

        if (vinculoAtivo)
            throw new InvalidOperationException("O aluno já possui enturmação ativa.");

        _context.Set<AlunoTurma>().Add(new AlunoTurma
        {
            AlunoId = alunoId,
            TurmaId = turmaId,
            DataInicio = dataInicio,
        });

        aluno.Status = Aluno.StatusAtivo;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RemanejarAsync(AlunoTurma vinculoOrigem, int turmaDestinoId, DateOnly dataMovimentacao)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var origem = await _context.Set<AlunoTurma>()
            .FirstAsync(x => x.Id == vinculoOrigem.Id && x.DataFim == null);

        var turmaDestino = await _context.Turmas.FirstAsync(x => x.Id == turmaDestinoId);
        var ocupacao = await _context.Set<AlunoTurma>().CountAsync(x =>
            x.TurmaId == turmaDestinoId &&
            x.DataInicio <= dataMovimentacao &&
            (x.DataFim == null || x.DataFim >= dataMovimentacao));

        if (ocupacao >= turmaDestino.VagasOfertadas)
            throw new InvalidOperationException("A turma de destino não possui vagas disponíveis para remanejamento.");

        origem.DataFim = dataMovimentacao.AddDays(-1);
        await _context.Set<AlunoTurma>().AddAsync(new AlunoTurma
        {
            AlunoId = origem.AlunoId,
            TurmaId = turmaDestinoId,
            DataInicio = dataMovimentacao,
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}