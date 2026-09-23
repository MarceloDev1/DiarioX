using DiarioX.Server.Application.DTOs.Turmas;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurmasController : ControllerBase
{
    private readonly ITurmaService _service;

    public TurmasController(ITurmaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var turmas = await _service.GetAllAsync();
        return Ok(turmas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var turma = await _service.GetByIdAsync(id);
        if (turma is null)
            return NotFound(new { message = "Turma não encontrada." });

        return Ok(turma);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TurmaRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Turma!.Id }, result.Turma);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TurmaRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.Turma);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(TurmaCommandResult result)
    {
        return result.Error switch
        {
            TurmaResultError.NotFound => NotFound(new { message = result.Message }),
            TurmaResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}