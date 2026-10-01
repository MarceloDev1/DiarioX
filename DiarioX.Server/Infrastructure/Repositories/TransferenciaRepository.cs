using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class TransferenciaRepository : ITransferenciaRepository
{
    private readonly AppDbContext _context;

    public TransferenciaRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Transferencia?> GetByIdAsync(int id)
    {
        return Query().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IReadOnlyList<Transferencia>> GetByAlunoIdAsync(int alunoId)
    {
        return await Query()
            .Where(t => t.AlunoId == alunoId)
            .OrderByDescending(t => t.DataTransferencia).ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    public Task<bool> ExistsByAlunoIdAsync(int alunoId)
    {
        return _context.Transferencias.IgnoreQueryFilters([AppDbContext.FiltroEscola]).AnyAsync(t => t.AlunoId == alunoId);
    }

    public async Task<Transferencia> TransferirAsync(Transferencia transferencia, int? vinculoEsperadoId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Trava o aluno: duas transferências simultâneas do mesmo aluno não passam ambas pela checagem.
        if (_context.Database.IsRelational())
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM alunos WHERE id = {transferencia.AlunoId} FOR UPDATE");

        var aluno = await _context.Alunos
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .FirstAsync(a => a.Id == transferencia.AlunoId);
        if (aluno.Status == Aluno.StatusTransferido)
            throw new InvalidOperationException("Este aluno já possui o status de Transferido no sistema.");

        var vinculo = await _context.AlunosTurmas
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .FirstOrDefaultAsync(x => x.AlunoId == aluno.Id && x.DataFim == null);
        if (vinculo?.Id != vinculoEsperadoId)
            throw new InvalidOperationException("A enturmação do aluno mudou enquanto a transferência era preenchida. Atualize a tela e tente novamente.");

        if (vinculo is not null)
        {
            // Mesma regra da desenturmação: o último dia na turma é a véspera da transferência; um vínculo
            // que começaria nessa data termina na véspera do início e não vale para nenhum dia.
            var ultimoDia = transferencia.DataTransferencia.AddDays(-1);
            var vesperaDoInicio = vinculo.DataInicio.AddDays(-1);
            vinculo.DataFim = ultimoDia > vesperaDoInicio ? ultimoDia : vesperaDoInicio;
            vinculo.MotivoDesenturmacao = AlunoTurma.MotivoTransferencia;
        }

        aluno.Status = Aluno.StatusTransferido;
        aluno.UpdatedAt = DateTime.UtcNow;
        _context.Transferencias.Add(transferencia);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return transferencia;
    }

    private IQueryable<Transferencia> Query()
    {
        // Vale o escopo da escola de origem. O filtro padrão também se aplicaria ao aluno incluído, e um
        // aluno readmitido em outra escola faria a transferência sumir para quem a registrou.
        return _context.Transferencias
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .AsNoTracking()
            .Include(t => t.Aluno)
            .Include(t => t.EscolaOrigem)
            .Include(t => t.Turma)
            .Include(t => t.AnoLetivo)
            .Where(t => !_context.EscopoPorEscola || _context.EscolasPermitidas.Contains(t.EscolaOrigemId));
    }
}
