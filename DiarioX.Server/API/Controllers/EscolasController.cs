using DiarioX.Server.Application.DTOs.Escolas;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EscolasController : ControllerBase
{
    private readonly IEscolaService _escolaService;

    public EscolasController(IEscolaService escolaService)
    {
        _escolaService = escolaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var escolas = await _escolaService.GetAllAsync();
        return Ok(escolas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var escola = await _escolaService.GetByIdAsync(id);
        if (escola is null)
            return NotFound(new { message = "Escola nao encontrada." });

        return Ok(escola);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EscolaRequest request)
    {
        var result = await _escolaService.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Escola!.Id }, result.Escola);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EscolaRequest request)
    {
        var result = await _escolaService.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.Escola);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _escolaService.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(EscolaCommandResult result)
    {
        return result.Error switch
        {
            EscolaResultError.NotFound => NotFound(new { message = result.Message }),
            EscolaResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
