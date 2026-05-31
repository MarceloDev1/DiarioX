using DiarioX.Server.Application.DTOs.EtapasEnsino;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EtapasEnsinoController : ControllerBase
{
    private readonly IEtapaEnsinoService _service;

    public EtapasEnsinoController(IEtapaEnsinoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var etapas = await _service.GetAllAsync();
        return Ok(etapas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var etapa = await _service.GetByIdAsync(id);
        if (etapa is null)
            return NotFound(new { message = "Etapa de ensino não encontrada." });

        return Ok(etapa);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EtapaEnsinoRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.EtapaEnsino!.Id }, result.EtapaEnsino);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EtapaEnsinoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.EtapaEnsino);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(EtapaEnsinoCommandResult result)
    {
        return result.Error switch
        {
            EtapaEnsinoResultError.NotFound => NotFound(new { message = result.Message }),
            EtapaEnsinoResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
