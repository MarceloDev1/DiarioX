using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiarioX.Server.Infrastructure.Repositories;

public class FaturamentoRepository : IFaturamentoRepository
{
    private readonly AppDbContext _context;

    public FaturamentoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PlanoAssinatura>> ListarPlanosAsync()
        => await _context.PlanosAssinatura.OrderBy(p => p.Nome).ToListAsync();

    public Task<PlanoAssinatura?> ObterPlanoAsync(int id)
        => _context.PlanosAssinatura.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExistePlanoComNomeAsync(string nome, int? excetoId = null)
        => _context.PlanosAssinatura.AnyAsync(p => p.Nome.ToLower() == nome.ToLower() && p.Id != excetoId);

    public async Task<IReadOnlyList<Tenant>> ListarInstituicoesAsync()
        => await _context.Tenants.OrderBy(t => t.Nome).ToListAsync();

    public Task<Tenant?> ObterInstituicaoAsync(int tenantId)
        => _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);

    public async Task<IReadOnlyList<Assinatura>> ListarAssinaturasAsync()
        => await _context.Assinaturas.Include(a => a.Plano).Include(a => a.Tenant).ToListAsync();

    public Task<Assinatura?> ObterAssinaturaPorInstituicaoAsync(int tenantId)
        => _context.Assinaturas.Include(a => a.Plano).Include(a => a.Tenant).FirstOrDefaultAsync(a => a.TenantId == tenantId);

    public async Task<IReadOnlyList<FaturaAssinatura>> ListarFaturasAsync(int? tenantId = null, string? situacao = null)
    {
        var query = FaturasQuery();
        if (tenantId is not null)
            query = query.Where(f => f.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(situacao))
            query = query.Where(f => f.Situacao == situacao);

        return await query.OrderByDescending(f => f.Vencimento).ThenBy(f => f.Id).ToListAsync();
    }

    public Task<FaturaAssinatura?> ObterFaturaAsync(int id)
        => FaturasQuery().FirstOrDefaultAsync(f => f.Id == id);

    public Task<FaturaAssinatura?> ObterFaturaPorCobrancaAsync(string asaasCobrancaId)
        => FaturasQuery().FirstOrDefaultAsync(f => f.AsaasCobrancaId == asaasCobrancaId);

    public Task<FaturaAssinatura?> ObterFaturaPorNotaFiscalAsync(string notaFiscalId)
        => FaturasQuery().FirstOrDefaultAsync(f => f.NotaFiscalId == notaFiscalId);

    public Task<bool> ExisteFaturaAsync(int assinaturaId, DateOnly competencia, bool incluirCanceladas)
        => _context.FaturasAssinatura.AnyAsync(f =>
            f.AssinaturaId == assinaturaId && f.Competencia == competencia &&
            (incluirCanceladas || f.Situacao != FaturaAssinatura.SituacaoCancelada));

    public async Task<IReadOnlyDictionary<int, int>> ContarAlunosAtivosAsync(DateOnly data)
    {
        // Roda também fora de uma requisição (rotina): ignora o filtro de instituição e agrupa por ela.
        return await _context.AlunosTurmas
            .IgnoreQueryFilters()
            .Where(at => at.DataInicio <= data && (at.DataFim == null || at.DataFim >= data))
            .Join(_context.Alunos.IgnoreQueryFilters().Where(a => a.Status != Aluno.StatusInativo),
                at => at.AlunoId, a => a.Id, (at, a) => new { at.TenantId, at.AlunoId })
            .Distinct()
            .GroupBy(x => x.TenantId)
            .Select(g => new { TenantId = g.Key, Quantidade = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Quantidade);
    }

    public void Adicionar<T>(T entidade) where T : class
        => _context.Set<T>().Add(entidade);

    public Task SalvarAsync()
        => _context.SaveChangesAsync();

    private IQueryable<FaturaAssinatura> FaturasQuery()
        => _context.FaturasAssinatura.Include(f => f.Assinatura).ThenInclude(a => a.Tenant);
}
