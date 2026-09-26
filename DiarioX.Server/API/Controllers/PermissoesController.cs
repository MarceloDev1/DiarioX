using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Permissoes;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissoesController : ControllerBase
{
    private readonly IPermissaoService _permissaoService;

    public PermissoesController(IPermissaoService permissaoService)
    {
        _permissaoService = permissaoService;
    }

    /// <summary>Permissões efetivas do usuário logado, usadas pelo frontend para montar menu e ações.</summary>
    [HttpGet("minhas")]
    public async Task<IActionResult> GetMinhas()
    {
        var isGlobalAdmin = User.HasClaim(AppClaimTypes.GlobalAdmin, "true");
        if (!int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var usuarioId))
            return Unauthorized();

        var permissoes = await _permissaoService.GetPermissoesDoUsuarioAsync(usuarioId, isGlobalAdmin);
        return Ok(permissoes.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Permissao(Permissoes.Configuracoes.Visualizar)]
    [HttpGet("catalogo")]
    public IActionResult GetCatalogo()
    {
        return Ok(_permissaoService.GetCatalogo());
    }

    [Permissao(Permissoes.Configuracoes.Visualizar)]
    [HttpGet("perfis")]
    public async Task<IActionResult> GetPerfis()
    {
        return Ok(await _permissaoService.GetPerfisAsync());
    }

    [Permissao(Permissoes.Configuracoes.Editar)]
    [HttpPut("perfis/{perfilId:int}")]
    public async Task<IActionResult> UpdatePerfil([FromRoute] int perfilId, [FromBody] PerfilPermissoesRequest request)
    {
        var result = await _permissaoService.UpdatePerfilAsync(perfilId, request);
        if (!result.Success)
        {
            return result.Error == PermissaoResultError.NotFound
                ? NotFound(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }

        return Ok(result.Perfil);
    }
}
