using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

/// <summary>
/// Faturamento da plataforma (planos, assinaturas e faturas). As entidades retornadas são
/// rastreadas: altere-as e chame SalvarAsync.
/// </summary>
public interface IFaturamentoRepository
{
    Task<IReadOnlyList<PlanoAssinatura>> ListarPlanosAsync();
    Task<PlanoAssinatura?> ObterPlanoAsync(int id);
    Task<bool> ExistePlanoComNomeAsync(string nome, int? excetoId = null);

    Task<IReadOnlyList<Tenant>> ListarInstituicoesAsync();
    Task<Tenant?> ObterInstituicaoAsync(int tenantId);

    Task<IReadOnlyList<Assinatura>> ListarAssinaturasAsync();
    Task<Assinatura?> ObterAssinaturaPorInstituicaoAsync(int tenantId);

    Task<IReadOnlyList<FaturaAssinatura>> ListarFaturasAsync(int? tenantId = null, string? situacao = null);
    Task<FaturaAssinatura?> ObterFaturaAsync(int id);
    Task<FaturaAssinatura?> ObterFaturaPorCobrancaAsync(string asaasCobrancaId);
    Task<FaturaAssinatura?> ObterFaturaPorNotaFiscalAsync(string notaFiscalId);

    /// <summary>Indica se há fatura na competência; canceladas contam só quando incluirCanceladas.</summary>
    Task<bool> ExisteFaturaAsync(int assinaturaId, DateOnly competencia, bool incluirCanceladas);

    /// <summary>Alunos não inativos com enturmação vigente na data, por instituição.</summary>
    Task<IReadOnlyDictionary<int, int>> ContarAlunosAtivosAsync(DateOnly data);

    void Adicionar<T>(T entidade) where T : class;
    Task SalvarAsync();
}
