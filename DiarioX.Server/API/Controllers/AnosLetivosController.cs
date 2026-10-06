using DiarioX.Server.Application.Auth;
using DiarioX.Server.Infrastructure.Authorization;
using DiarioX.Server.Application.DTOs.AnosLetivos;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnosLetivosController : ControllerBase
{
    private readonly IAnoLetivoService _service;

    public AnosLetivosController(IAnoLetivoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var anos = await _service.GetAllAsync();
        return Ok(anos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var ano = await _service.GetByIdAsync(id);
        if (ano is null)
            return NotFound(new { message = "Ano letivo não encontrado." });
        return Ok(ano);
    }

    [Permissao(Permissoes.AnosLetivos.Criar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AnoLetivoRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);
        return CreatedAtAction(nameof(GetById), new { id = result.AnoLetivo!.Id }, result.AnoLetivo);
    }

    [Permissao(Permissoes.AnosLetivos.Editar)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AnoLetivoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);
        return Ok(result.AnoLetivo);
    }

    [Permissao(Permissoes.AnosLetivos.Excluir)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);
        return Ok(new { message = result.Message });
    }

    /// <summary>Encerra o período avaliativo: o diário de classe deixa de aceitar alterações nele (RF017 EX02).</summary>
    [Permissao(Permissoes.AnosLetivos.Editar)]
    [HttpPost("{id:int}/periodos/{periodoId:int}/encerrar")]
    public async Task<IActionResult> EncerrarPeriodo([FromRoute] int id, [FromRoute] int periodoId)
    {
        var result = await _service.DefinirPeriodoEncerradoAsync(id, periodoId, encerrado: true);
        return result.Success ? Ok(result.AnoLetivo) : MapError(result);
    }

    /// <summary>Reabre o período avaliativo encerrado.</summary>
    [Permissao(Permissoes.AnosLetivos.Editar)]
    [HttpPost("{id:int}/periodos/{periodoId:int}/reabrir")]
    public async Task<IActionResult> ReabrirPeriodo([FromRoute] int id, [FromRoute] int periodoId)
    {
        var result = await _service.DefinirPeriodoEncerradoAsync(id, periodoId, encerrado: false);
        return result.Success ? Ok(result.AnoLetivo) : MapError(result);
    }

    private IActionResult MapError(AnoLetivoCommandResult result) =>
        result.Error switch
        {
            AnoLetivoResultError.NotFound => NotFound(new { message = result.Message }),
            AnoLetivoResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
}
