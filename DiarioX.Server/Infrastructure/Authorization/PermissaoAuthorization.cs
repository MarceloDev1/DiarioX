using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DiarioX.Server.Infrastructure.Authorization;

/// <summary>
/// Exige ao menos uma das permissões informadas (ex.: [Permissao(Permissoes.Alunos.Criar)]).
/// Mantém a exigência padrão de usuário autenticado atuando em uma instituição.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissaoAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "permissao:";

    public PermissaoAttribute(params string[] permissoes)
    {
        Policy = PolicyPrefix + string.Join('|', permissoes);
    }
}

public sealed class PermissaoRequirement : IAuthorizationRequirement
{
    public PermissaoRequirement(IReadOnlyList<string> permissoes)
    {
        Permissoes = permissoes;
    }

    public IReadOnlyList<string> Permissoes { get; }
}

/// <summary>Monta sob demanda as políticas "permissao:..." geradas pelo PermissaoAttribute.</summary>
public sealed class PermissaoPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissaoPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PermissaoAttribute.PolicyPrefix, StringComparison.Ordinal))
            return await base.GetPolicyAsync(policyName);

        var permissoes = policyName[PermissaoAttribute.PolicyPrefix.Length..]
            .Split('|', StringSplitOptions.RemoveEmptyEntries);

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(AppClaimTypes.TenantId)
            .AddRequirements(new PermissaoRequirement(permissoes))
            .Build();
    }
}

/// <summary>
/// Consulta as permissões a cada requisição, então mudanças na matriz valem sem novo login.
/// </summary>
public sealed class PermissaoHandler : AuthorizationHandler<PermissaoRequirement>
{
    private readonly IPermissaoService _permissaoService;

    public PermissaoHandler(IPermissaoService permissaoService)
    {
        _permissaoService = permissaoService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
    {
        var isGlobalAdmin = context.User.HasClaim(AppClaimTypes.GlobalAdmin, "true");
        if (isGlobalAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        if (!int.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var usuarioId))
            return;

        var permissoes = await _permissaoService.GetPermissoesDoUsuarioAsync(usuarioId, isGlobalAdmin: false);
        if (requirement.Permissoes.Any(permissoes.Contains))
            context.Succeed(requirement);
    }
}
