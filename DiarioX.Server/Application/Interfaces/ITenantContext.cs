namespace DiarioX.Server.Application.Interfaces;

/// <summary>
/// Instituição (tenant) da requisição atual, resolvida pelo token (requisições autenticadas)
/// ou pelo subdomínio (login, primeiro acesso e recuperação de senha).
/// Nulo na área global: Administrador sem instituição selecionada, host de administração
/// ou processos internos como o seed.
/// </summary>
public interface ITenantContext
{
    int? TenantId { get; }
    string? TenantSlug { get; }

    /// <summary>
    /// Escolas da instituição que o usuário da requisição pode acessar. Nulo = todas (sem restrição),
    /// que é o caso do Administrador global, da gerência da rede e dos processos internos.
    /// </summary>
    IReadOnlyList<int>? EscolaIds => null;
}
