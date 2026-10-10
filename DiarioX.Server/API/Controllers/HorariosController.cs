using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Horarios;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

/// <summary>Grade semanal de tempos de aula das turmas.</summary>
[ApiController]
[Route("api/[controller]")]
public class HorariosController : ControllerBase
{
    private readonly IHorarioAulaService _service;

    public HorariosController(IHorarioAulaService service)
    {
        _service = service;
    }

    [Permissao(Permissoes.Horarios.Visualizar)]
    [HttpGet("turmas")]
    public async Task<IActionResult> GetTurmas()
        => Ok(await _service.GetTurmasAsync());

    [Permissao(Permissoes.Horarios.Visualizar)]
    [HttpGet("turmas/{turmaId:int}")]
    public async Task<IActionResult> Get([FromRoute] int turmaId)
        => FromResult(await _service.GetAsync(turmaId));

    [Permissao(Permissoes.Horarios.Editar)]
    [HttpPut("turmas/{turmaId:int}")]
    public async Task<IActionResult> Salvar([FromRoute] int turmaId, [FromBody] HorarioRequest request)
        => FromResult(await _service.SalvarAsync(turmaId, request));

    private IActionResult FromResult<T>(HorarioResult<T> result) => result.Error switch
    {
        HorarioResultError.None => Ok(result.Value),
        HorarioResultError.NotFound => NotFound(new { message = result.Message }),
        _ => BadRequest(new { message = result.Message }),
    };
}
