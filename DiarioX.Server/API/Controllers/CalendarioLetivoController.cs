using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Calendario;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarioLetivoController : ControllerBase
{
    private readonly ICalendarioLetivoService _service;

    public CalendarioLetivoController(ICalendarioLetivoService service)
    {
        _service = service;
    }

    /// <summary>Anos letivos e escolas que o usuário pode selecionar.</summary>
    [Permissao(Permissoes.CalendarioLetivo.Visualizar)]
    [HttpGet("opcoes")]
    public async Task<IActionResult> GetOpcoes()
    {
        return Ok(await _service.GetOpcoesAsync());
    }

    /// <summary>Calendário do ano letivo: o da escola ou, sem escolaId, o da rede.</summary>
    [Permissao(Permissoes.CalendarioLetivo.Visualizar)]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int anoLetivoId, [FromQuery] int? escolaId)
    {
        return FromResult(await _service.GetAsync(anoLetivoId, escolaId));
    }

    /// <summary>Cadastra o evento em um dia ou em todos os dias do intervalo, substituindo os existentes.</summary>
    [Permissao(Permissoes.CalendarioLetivo.Editar)]
    [HttpPost("eventos")]
    public async Task<IActionResult> SalvarEvento([FromBody] EventoCalendarioRequest request)
    {
        return FromResult(await _service.SalvarEventoAsync(request));
    }

    /// <summary>Remove os eventos do calendário no intervalo [de, ate] (sem ate: só o dia de).</summary>
    [Permissao(Permissoes.CalendarioLetivo.Editar)]
    [HttpDelete("eventos")]
    public async Task<IActionResult> RemoverEventos(
        [FromQuery] int anoLetivoId, [FromQuery] int? escolaId, [FromQuery] DateOnly de, [FromQuery] DateOnly? ate)
    {
        return FromResult(await _service.RemoverEventosAsync(anoLetivoId, escolaId, de, ate));
    }

    [Permissao(Permissoes.CalendarioLetivo.Editar)]
    [HttpPost("publicar")]
    public async Task<IActionResult> Publicar([FromBody] CalendarioPublicacaoRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromResult(await _service.PublicarAsync(usuario, request));
    }

    private IActionResult FromResult(CalendarioCommandResult result)
    {
        if (result.Success)
            return Ok(new { message = result.Message, calendario = result.Calendario });

        return result.Error switch
        {
            CalendarioResultError.NotFound => NotFound(new { message = result.Message }),
            CalendarioResultError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
