using DiarioX.Server.Application.Auth;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Infrastructure.Tenancy;

/// <summary>
/// Define a instituição de cada requisição da API. Deve rodar depois de UseAuthentication.
/// - Autenticada: a instituição vem exclusivamente do token (claims tenant_id/tenant_slug).
/// - Anônima (login, primeiro acesso, senha): vem do subdomínio do host.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        TenantHostResolver hostResolver,
        ITenantRepository tenantRepository)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst(AppClaimTypes.TenantId)?.Value;
            var tenantSlugClaim = context.User.FindFirst(AppClaimTypes.TenantSlug)?.Value;

            if (int.TryParse(tenantIdClaim, out var tenantId) && tenantSlugClaim is not null)
                tenantContext.SetTenant(tenantId, tenantSlugClaim);

            await _next(context);
            return;
        }

        var slug = hostResolver.GetTenantSlug(context.Request.Host.Host);
        if (slug is not null)
        {
            var tenant = await tenantRepository.GetBySlugAsync(slug);
            if (tenant is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { message = "Instituição não encontrada." });
                return;
            }

            if (tenant.Status != Tenant.StatusAtivo)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "Instituição inativa. Entre em contato com o suporte." });
                return;
            }

            tenantContext.SetTenant(tenant.Id, tenant.Slug);
        }

        await _next(context);
    }
}
