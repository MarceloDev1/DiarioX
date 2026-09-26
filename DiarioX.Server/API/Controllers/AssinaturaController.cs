using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

/// <summary>Assinatura do Diário X vista pela própria instituição.</summary>
[ApiController]
[Route("api/[controller]")]
public class AssinaturaController : ControllerBase
{
    private readonly IFaturamentoService _faturamentoService;
    private readonly IPermissaoService _permissaoService;
    private readonly ITenantContext _tenantContext;

    public AssinaturaController(
        IFaturamentoService faturamentoService,
        IPermissaoService permissaoService,
        ITenantContext tenantContext)
    {
        _faturamentoService = faturamentoService;
        _permissaoService = permissaoService;
        _tenantContext = tenantContext;
    }

    /// <summary>Situação financeira para o aviso no topo do sistema (qualquer usuário da instituição).</summary>
    [HttpGet("aviso")]
    public async Task<IActionResult> GetAviso()
    {
        if (_tenantContext.TenantId is not int tenantId || !User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var permissoes = await _permissaoService.GetPermissoesDoUsuarioAsync(usuario.UsuarioId, usuario.IsGlobalAdmin);
        var podeVerFaturas = permissoes.Contains(Permissoes.Configuracoes.Visualizar);

        return Ok(await _faturamentoService.ObterAvisoAsync(tenantId, podeVerFaturas, DateOnly.FromDateTime(DateTime.Today)));
    }

    /// <summary>Plano e faturas da instituição, com links de pagamento e notas fiscais.</summary>
    [Permissao(Permissoes.Configuracoes.Visualizar)]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (_tenantContext.TenantId is not int tenantId)
            return Unauthorized();

        var assinatura = await _faturamentoService.ObterMinhaAssinaturaAsync(tenantId);
        return assinatura is null ? NoContent() : Ok(assinatura);
    }
}
