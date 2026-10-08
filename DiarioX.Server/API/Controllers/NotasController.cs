using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Notas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/notas")]
public class NotasController : ControllerBase
{
    private readonly INotaService _notaService;

    public NotasController(INotaService notaService)
    {
        _notaService = notaService;
    }

    /// <summary>Turmas, disciplinas e períodos disponíveis para o usuário.</summary>
    [Permissao(Permissoes.Notas.Visualizar)]
    [HttpGet("turmas")]
    public async Task<IActionResult> GetTurmas()
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return Ok(await _notaService.GetTurmasAsync(usuario));
    }

    /// <summary>Avaliações do período com as notas e a nota do período de cada aluno.</summary>
    [Permissao(Permissoes.Notas.Visualizar)]
    [HttpGet("periodo")]
    public async Task<IActionResult> GetPeriodo([FromQuery] int turmaId, [FromQuery] int disciplinaId, [FromQuery] int periodoId)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromResult(await _notaService.GetPeriodoAsync(usuario, turmaId, disciplinaId, periodoId));
    }

    /// <summary>Notas de cada período e média do ano, por aluno.</summary>
    [Permissao(Permissoes.Notas.Visualizar)]
    [HttpGet("medias")]
    public async Task<IActionResult> GetMedias([FromQuery] int turmaId, [FromQuery] int disciplinaId)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromResult(await _notaService.GetMediasAsync(usuario, turmaId, disciplinaId));
    }

    [Permissao(Permissoes.Notas.Criar)]
    [HttpPost("avaliacoes")]
    public async Task<IActionResult> CriarAvaliacao([FromBody] AvaliacaoRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromResult(await _notaService.SalvarAvaliacaoAsync(usuario, null, request));
    }

    [Permissao(Permissoes.Notas.Editar)]
    [HttpPut("avaliacoes/{id:int}")]
    public async Task<IActionResult> AlterarAvaliacao([FromRoute] int id, [FromBody] AvaliacaoRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromResult(await _notaService.SalvarAvaliacaoAsync(usuario, id, request));
    }

    [Permissao(Permissoes.Notas.Excluir)]
    [HttpDelete("avaliacoes/{id:int}")]
    public async Task<IActionResult> ExcluirAvaliacao([FromRoute] int id)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _notaService.ExcluirAvaliacaoAsync(usuario, id);
        return result.Success ? Ok(new { message = result.Message }) : MapError(result.Error, result.Message);
    }

    /// <summary>Lança, altera ou apaga (valor nulo) notas das avaliações de um período.</summary>
    [Permissao(Permissoes.Notas.Criar)]
    [HttpPut("lancamentos")]
    public async Task<IActionResult> Lancar([FromBody] LancamentoNotasRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromResult(await _notaService.LancarNotasAsync(usuario, request));
    }

    private IActionResult FromResult<T>(NotaResult<T> result)
        => result.Success ? Ok(result.Value) : MapError(result.Error, result.Message);

    private IActionResult MapError(NotaResultError error, string message)
    {
        return error switch
        {
            NotaResultError.NotFound => NotFound(new { message }),
            NotaResultError.Conflict => Conflict(new { message }),
            NotaResultError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message }),
            _ => BadRequest(new { message })
        };
    }
}
