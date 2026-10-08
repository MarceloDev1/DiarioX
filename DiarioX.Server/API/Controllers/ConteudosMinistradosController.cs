using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Conteudos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConteudosMinistradosController : ControllerBase
{
    private readonly IConteudoMinistradoService _service;

    public ConteudosMinistradosController(IConteudoMinistradoService service)
    {
        _service = service;
    }

    /// <summary>Turmas e disciplinas em que o usuário pode registrar conteúdo.</summary>
    [Permissao(Permissoes.ConteudoMinistrado.Visualizar)]
    [HttpGet("turmas")]
    public async Task<IActionResult> GetTurmas()
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return Ok(await _service.GetTurmasAsync(usuario));
    }

    /// <summary>Conteúdo da data (o registrado ou o formulário em branco).</summary>
    [Permissao(Permissoes.ConteudoMinistrado.Visualizar)]
    [HttpGet("aula")]
    public async Task<IActionResult> GetAula([FromQuery] int turmaId, [FromQuery] int? disciplinaId, [FromQuery] DateOnly data)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromQuery(await _service.GetAsync(usuario, turmaId, disciplinaId, data));
    }

    /// <summary>Diário: dias com frequência ou conteúdo, sinalizando a falta de um dos dois (RN01).</summary>
    [Permissao(Permissoes.ConteudoMinistrado.Visualizar)]
    [HttpGet("diario")]
    public async Task<IActionResult> GetDiario(
        [FromQuery] int turmaId, [FromQuery] int? disciplinaId, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromQuery(await _service.GetDiarioAsync(usuario, turmaId, disciplinaId, de, ate));
    }

    /// <summary>Habilidades da BNCC sugeridas para a etapa da turma e a disciplina (RN02).</summary>
    [Permissao(Permissoes.ConteudoMinistrado.Visualizar)]
    [HttpGet("sugestoes-bncc")]
    public async Task<IActionResult> GetSugestoes(
        [FromQuery] int turmaId, [FromQuery] int? disciplinaId, [FromQuery] string? busca)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return FromQuery(await _service.GetSugestoesAsync(usuario, turmaId, disciplinaId, busca));
    }

    [Permissao(Permissoes.ConteudoMinistrado.Criar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ConteudoMinistradoRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _service.CreateAsync(usuario, request);
        return result.Success ? Ok(result.Conteudo) : MapError(result.Error, result.Message);
    }

    [Permissao(Permissoes.ConteudoMinistrado.Editar)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ConteudoMinistradoRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _service.UpdateAsync(usuario, id, request);
        return result.Success ? Ok(result.Conteudo) : MapError(result.Error, result.Message);
    }

    [Permissao(Permissoes.ConteudoMinistrado.Excluir)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _service.DeleteAsync(usuario, id);
        return result.Success ? Ok(new { message = result.Message }) : MapError(result.Error, result.Message);
    }

    private IActionResult FromQuery<T>(ConteudoQueryResult<T> result)
        => result.Success ? Ok(result.Value) : MapError(result.Error, result.Message);

    private IActionResult MapError(ConteudoResultError error, string message)
    {
        return error switch
        {
            ConteudoResultError.NotFound => NotFound(new { message }),
            ConteudoResultError.Conflict => Conflict(new { message }),
            ConteudoResultError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message }),
            _ => BadRequest(new { message })
        };
    }
}
