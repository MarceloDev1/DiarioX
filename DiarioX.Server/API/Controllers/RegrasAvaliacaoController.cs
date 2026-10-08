using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.RegrasAvaliacao;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/regras-avaliacao")]
public class RegrasAvaliacaoController : ControllerBase
{
    private readonly IRegraAvaliacaoService _service;

    public RegrasAvaliacaoController(IRegraAvaliacaoService service)
    {
        _service = service;
    }

    [Permissao(Permissoes.RegrasAvaliacao.Visualizar)]
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Regra usada pelas etapas sem regra própria.</summary>
    [Permissao(Permissoes.RegrasAvaliacao.Visualizar)]
    [HttpGet("padrao")]
    public IActionResult GetPadrao() => Ok(_service.GetPadrao());

    [Permissao(Permissoes.RegrasAvaliacao.Visualizar)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var regra = await _service.GetByIdAsync(id);
        return regra is null ? NotFound(new { message = "Regra de avaliação não encontrada." }) : Ok(regra);
    }

    [Permissao(Permissoes.RegrasAvaliacao.Criar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegraAvaliacaoRequest request)
    {
        var result = await _service.CreateAsync(request);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Regra!.Id }, result.Regra)
            : MapError(result);
    }

    [Permissao(Permissoes.RegrasAvaliacao.Editar)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] RegraAvaliacaoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result.Success ? Ok(result.Regra) : MapError(result);
    }

    [Permissao(Permissoes.RegrasAvaliacao.Excluir)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : MapError(result);
    }

    private IActionResult MapError(RegraAvaliacaoCommandResult result)
    {
        return result.Error switch
        {
            RegraAvaliacaoResultError.NotFound => NotFound(new { message = result.Message }),
            RegraAvaliacaoResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
