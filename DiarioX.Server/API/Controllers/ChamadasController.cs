using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Chamadas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChamadasController : ControllerBase
{
    private readonly IChamadaService _chamadaService;

    public ChamadasController(IChamadaService chamadaService)
    {
        _chamadaService = chamadaService;
    }

    /// <summary>Turmas e disciplinas disponíveis para o usuário lançar chamada.</summary>
    [Permissao(Permissoes.Chamada.Visualizar)]
    [HttpGet("turmas")]
    public async Task<IActionResult> GetTurmas()
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return Ok(await _chamadaService.GetTurmasAsync(usuario));
    }

    /// <summary>Chamada da data (a registrada ou a lista de alunos em branco).</summary>
    [Permissao(Permissoes.Chamada.Visualizar)]
    [HttpGet("aula")]
    public async Task<IActionResult> GetAula([FromQuery] int turmaId, [FromQuery] int disciplinaId, [FromQuery] DateOnly data)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromQuery(await _chamadaService.GetAsync(usuario, turmaId, disciplinaId, data));
    }

    /// <summary>Histórico de chamadas da turma/disciplina.</summary>
    [Permissao(Permissoes.Chamada.Visualizar)]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int turmaId, [FromQuery] int disciplinaId)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromQuery(await _chamadaService.ListAsync(usuario, turmaId, disciplinaId));
    }

    [Permissao(Permissoes.Chamada.Visualizar)]
    [HttpGet("frequencia")]
    public async Task<IActionResult> GetFrequencia([FromQuery] int turmaId, [FromQuery] int disciplinaId, [FromQuery] int? periodoId)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromQuery(await _chamadaService.GetFrequenciaAsync(usuario, turmaId, disciplinaId, periodoId));
    }

    [Permissao(Permissoes.Chamada.Criar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChamadaRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _chamadaService.CreateAsync(usuario, request);
        return result.Success ? Ok(result.Chamada) : MapError(result.Error, result.Message);
    }

    [Permissao(Permissoes.Chamada.Editar)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ChamadaRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _chamadaService.UpdateAsync(usuario, id, request);
        return result.Success ? Ok(result.Chamada) : MapError(result.Error, result.Message);
    }

    [Permissao(Permissoes.Chamada.Excluir)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _chamadaService.DeleteAsync(usuario, id);
        return result.Success ? Ok(new { message = result.Message }) : MapError(result.Error, result.Message);
    }

    private IActionResult FromQuery<T>(ChamadaQueryResult<T> result)
        => result.Success ? Ok(result.Value) : MapError(result.Error, result.Message);

    private IActionResult MapError(ChamadaResultError error, string message)
    {
        return error switch
        {
            ChamadaResultError.NotFound => NotFound(new { message }),
            ChamadaResultError.Conflict => Conflict(new { message }),
            ChamadaResultError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message }),
            _ => BadRequest(new { message })
        };
    }
}
