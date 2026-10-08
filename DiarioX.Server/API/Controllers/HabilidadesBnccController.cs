using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Conteudos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HabilidadesBnccController : ControllerBase
{
    private readonly IHabilidadeBnccService _service;

    public HabilidadesBnccController(IHabilidadeBnccService service)
    {
        _service = service;
    }

    [Permissao(Permissoes.HabilidadesBncc.Visualizar)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [Permissao(Permissoes.HabilidadesBncc.Visualizar)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var habilidade = await _service.GetByIdAsync(id);
        if (habilidade is null)
            return NotFound(new { message = "Habilidade não encontrada." });

        return Ok(habilidade);
    }

    [Permissao(Permissoes.HabilidadesBncc.Criar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HabilidadeBnccRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Habilidade!.Id }, result.Habilidade);
    }

    [Permissao(Permissoes.HabilidadesBncc.Editar)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] HabilidadeBnccRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result.Success ? Ok(result.Habilidade) : MapError(result);
    }

    [Permissao(Permissoes.HabilidadesBncc.Excluir)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        return result.Success ? Ok(new { message = result.Message }) : MapError(result);
    }

    private IActionResult MapError(HabilidadeBnccCommandResult result)
    {
        return result.Error switch
        {
            HabilidadeBnccResultError.NotFound => NotFound(new { message = result.Message }),
            HabilidadeBnccResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
