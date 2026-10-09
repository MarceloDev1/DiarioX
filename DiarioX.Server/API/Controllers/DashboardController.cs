using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

/// <summary>
/// Painel da página inicial. Aberto a qualquer usuário da instituição: cada bloco é incluído
/// conforme as permissões do perfil.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IPainelProfessorService _painelProfessorService;

    public DashboardController(IDashboardService dashboardService, IPainelProfessorService painelProfessorService)
    {
        _dashboardService = dashboardService;
        _painelProfessorService = painelProfessorService;
    }

    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return Ok(await _dashboardService.ObterAsync(usuario, DateOnly.FromDateTime(DateTime.Today)));
    }

    /// <summary>Home do professor. 204 quando o usuário não é professor (a home usa o painel geral).</summary>
    [HttpGet("professor")]
    public async Task<IActionResult> ObterProfessor([FromQuery] int? escolaId)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var painel = await _painelProfessorService.ObterAsync(usuario, DateOnly.FromDateTime(DateTime.Today), escolaId);
        return painel is null ? NoContent() : Ok(painel);
    }

    /// <summary>Calendário de pendências de frequência e aula de um mês.</summary>
    [HttpGet("professor/pendencias")]
    public async Task<IActionResult> ObterPendencias([FromQuery] int ano, [FromQuery] int mes, [FromQuery] int? escolaId)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        if (ano is < 2000 or > 2100 || mes is < 1 or > 12)
            return BadRequest(new { message = "Mês inválido." });

        var pendencias = await _painelProfessorService.ObterPendenciasAsync(
            usuario, DateOnly.FromDateTime(DateTime.Today), escolaId, ano, mes);
        return pendencias is null ? NoContent() : Ok(pendencias);
    }
}
