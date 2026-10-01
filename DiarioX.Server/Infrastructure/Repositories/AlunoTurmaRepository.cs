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
        // Somente leitura: rastrear o Aluno incluído conflitaria com atualizações do mesmo aluno
        // na requisição (ex.: reativação); RemanejarAsync recarrega o vínculo antes de alterá-lo.
        // A enturmação ativa é única por aluno na instituição; vale mesmo fora do escopo do usuário.
        return _context.Set<AlunoTurma>()
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .AsNoTracking()
            .Include(x => x.Aluno).ThenInclude(x => x.Escola)
            .Include(x => x.Turma).ThenInclude(x => x.AnoLetivo)
            .Include(x => x.Turma).ThenInclude(x => x.Escola)
            .FirstOrDefaultAsync(x => x.AlunoId == alunoId && x.DataFim == null);
    }

    public async Task<IReadOnlyList<AlunoTurma>> GetAtivasByTurmaIdAsync(int turmaId)
    {
        // Vale a escola da turma (já validada pelo chamador), não a do cadastro do aluno.
        return await _context.Set<AlunoTurma>()
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .AsNoTracking()
            .Include(x => x.Aluno)
            .Where(x => x.TurmaId == turmaId && x.DataFim == null)
            .OrderBy(x => x.Aluno.Nome)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetAlunoIdsComEnturmacaoAtivaAsync(IReadOnlyCollection<int> alunoIds)
    {
        return await _context.Set<AlunoTurma>()
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .Where(x => alunoIds.Contains(x.AlunoId) && x.DataFim == null)
            .Select(x => x.AlunoId)
            .ToListAsync();
    }

    public Task<bool> ExistsByAlunoIdAsync(int alunoId)
    {
        // O histórico em escolas fora do escopo do usuário também impede a exclusão do aluno.
        return _context.Set<AlunoTurma>().IgnoreQueryFilters([AppDbContext.FiltroEscola]).AnyAsync(x => x.AlunoId == alunoId);
    }

    public async Task<bool> HasVacancyAsync(int turmaId, DateOnly dataMovimentacao)
    {
        var turma = await _context.Turmas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == turmaId);
        if (turma is null)
            return false;

        return await GetOcupacaoMaximaAsync(turmaId, dataMovimentacao) < turma.VagasOfertadas;
    }

    public async Task<int> GetOcupacaoMaximaAsync(int turmaId, DateOnly aPartirDe)
    {
        // Um vínculo iniciado em aPartirDe vale dali em diante. Se a data for retroativa, ele precisa
        // caber também nas datas seguintes, quando outros alunos já podem ter entrado na turma: a
        // ocupação só aumenta nos inícios de vínculo, então basta medi-la em aPartirDe e em cada um deles.
        var periodos = await _context.Set<AlunoTurma>()
            .AsNoTracking()
            .Where(x => x.TurmaId == turmaId && (x.DataFim == null || x.DataFim >= aPartirDe))
            .Select(x => new { x.DataInicio, x.DataFim })
            .ToListAsync();

        return periodos
            .Select(p => p.DataInicio > aPartirDe ? p.DataInicio : aPartirDe)
            .Distinct()
            .Select(data => periodos.Count(p => p.DataInicio <= data && (p.DataFim == null || p.DataFim >= data)))
            .DefaultIfEmpty(0)
            .Max();
    }

    public async Task<bool> EnturmarAsync(IReadOnlyCollection<int> alunoIds, int turmaId, DateOnly dataInicio)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var turma = await BloquearTurmaAsync(turmaId);

        var alunos = await _context.Set<Aluno>().Where(x => alunoIds.Contains(x.Id)).ToListAsync();
        if (alunos.Count != alunoIds.Count)
            throw new InvalidOperationException("Um ou mais alunos não foram encontrados.");

        if ((await GetAlunoIdsComEnturmacaoAtivaAsync(alunoIds)).Count > 0)
            throw new InvalidOperationException(alunoIds.Count == 1
                ? "O aluno já possui enturmação ativa."
                : "Um ou mais alunos já possuem enturmação ativa.");

        if (await GetOcupacaoMaximaAsync(turmaId, dataInicio) + alunoIds.Count > turma.VagasOfertadas)
            return false;

        foreach (var aluno in alunos)
        {
            _context.Set<AlunoTurma>().Add(new AlunoTurma
            {
                AlunoId = aluno.Id,
                TurmaId = turmaId,
                DataInicio = dataInicio,
            });
            aluno.Status = Aluno.StatusAtivo;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task RemanejarAsync(AlunoTurma vinculoOrigem, int turmaDestinoId, DateOnly dataMovimentacao)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var turmaDestino = await BloquearTurmaAsync(turmaDestinoId);

        var origem = await _context.Set<AlunoTurma>()
            .FirstAsync(x => x.Id == vinculoOrigem.Id && x.DataFim == null);

        if (await GetOcupacaoMaximaAsync(turmaDestinoId, dataMovimentacao) >= turmaDestino.VagasOfertadas)
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

    public async Task DesenturmarAsync(int turmaId, IReadOnlyDictionary<int, string> statusPorAluno, DateOnly dataDesenturmacao,
        string motivo, string? observacao)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        await BloquearTurmaAsync(turmaId);

        var alunoIds = statusPorAluno.Keys.ToList();
        var vinculos = await _context.Set<AlunoTurma>()
            .IgnoreQueryFilters([AppDbContext.FiltroEscola])
            .Include(x => x.Aluno)
            .Where(x => x.TurmaId == turmaId && x.DataFim == null && alunoIds.Contains(x.AlunoId))
            .ToListAsync();

        if (vinculos.Count != alunoIds.Count)
            throw new InvalidOperationException(alunoIds.Count == 1
                ? "O aluno não está mais enturmado nesta turma. Atualize a tela e tente novamente."
                : "Um ou mais alunos não estão mais enturmados nesta turma. Atualize a tela e tente novamente.");

        // A vaga fica livre já em dataDesenturmacao (RN03). Um vínculo que começaria nessa data (ou depois)
        // termina na véspera do início: continua no histórico, com o motivo, sem valer para nenhum dia.
        var ultimoDia = dataDesenturmacao.AddDays(-1);
        foreach (var vinculo in vinculos)
        {
            var vesperaDoInicio = vinculo.DataInicio.AddDays(-1);
            vinculo.DataFim = ultimoDia > vesperaDoInicio ? ultimoDia : vesperaDoInicio;
            vinculo.MotivoDesenturmacao = motivo;
            vinculo.ObservacaoDesenturmacao = observacao;
            vinculo.Aluno.Status = statusPorAluno[vinculo.AlunoId];
            vinculo.Aluno.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Trava a linha da turma até o fim da transação, para que enturmações simultâneas na mesma
    /// turma contem as vagas uma depois da outra, em vez de ambas passarem pela checagem.
    /// </summary>
    private async Task<Turma> BloquearTurmaAsync(int turmaId)
    {
        if (_context.Database.IsRelational())
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM turmas WHERE id = {turmaId} FOR UPDATE");

        return await _context.Turmas.AsNoTracking().FirstAsync(x => x.Id == turmaId);
    }
}
