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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AnoLetivoRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);
        return CreatedAtAction(nameof(GetById), new { id = result.AnoLetivo!.Id }, result.AnoLetivo);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AnoLetivoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);
        return Ok(result.AnoLetivo);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);
        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(AnoLetivoCommandResult result) =>
        result.Error switch
        {
            AnoLetivoResultError.NotFound => NotFound(new { message = result.Message }),
            AnoLetivoResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
}
