using System.IdentityModel.Tokens.Jwt;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Infrastructure.Tenancy;

/// <summary>
/// Define a instituição de cada requisição da API. Deve rodar depois de UseAuthentication.
/// - Autenticada: a instituição vem exclusivamente do token (claims tenant_id/tenant_slug).
/// - Anônima (login, primeiro acesso, senha): vem do subdomínio do host.
/// Nas autenticadas também define as escolas que o usuário pode acessar (EscopoEscolaResolver).
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
        ITenantRepository tenantRepository,
        EscopoEscolaResolver escopoEscolaResolver)
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
            {
                tenantContext.SetTenant(tenantId, tenantSlugClaim);

                if (await BloqueadaParaGravacaoAsync(context, tenantId, tenantRepository))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "A instituição está em modo somente leitura por pendência financeira com o Diário X. " +
                                  "Consultas e chamadas continuam liberadas; as alterações voltam assim que o pagamento for confirmado.",
                        codigo = Tenant.SituacaoFinanceiraSomenteLeitura,
                    });
                    return;
                }

                // O Administrador global acessa todas as escolas da instituição selecionada.
                var usuarioIdClaim = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!context.User.HasClaim(AppClaimTypes.GlobalAdmin, "true") && int.TryParse(usuarioIdClaim, out var usuarioId))
                    tenantContext.SetEscolas(await escopoEscolaResolver.ResolverAsync(usuarioId));
            }

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

    // Em somente leitura, a instituição só consulta, com exceção da chamada (para não prejudicar as
    // aulas) e da autenticação. O Administrador global não é bloqueado.
    private static readonly string[] RotasLiberadasEmSomenteLeitura = ["/api/auth", "/api/chamadas"];

    private static async Task<bool> BloqueadaParaGravacaoAsync(HttpContext context, int tenantId, ITenantRepository tenantRepository)
    {
        var request = context.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method))
            return false;

        if (context.User.HasClaim(AppClaimTypes.GlobalAdmin, "true"))
            return false;

        if (RotasLiberadasEmSomenteLeitura.Any(rota => request.Path.StartsWithSegments(rota)))
            return false;

        var tenant = await tenantRepository.GetByIdAsync(tenantId);
        return tenant?.SituacaoFinanceira == Tenant.SituacaoFinanceiraSomenteLeitura;
    }
}
