using DiarioX.Server.Application.Interfaces;

namespace DiarioX.Server.Infrastructure.Tenancy;

/// <summary>
/// Instituição da requisição atual (scoped). Preenchida pelo TenantResolutionMiddleware;
/// fora de uma requisição HTTP (seed, migrations) permanece vazia, isto é, área global.
/// </summary>
public class TenantContext : ITenantContext
{
    public int? TenantId { get; private set; }
    public string? TenantSlug { get; private set; }

    public void SetTenant(int tenantId, string slug)
    {
        TenantId = tenantId;
        TenantSlug = slug;
    }
}
