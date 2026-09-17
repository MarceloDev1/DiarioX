using DiarioX.Server.Application.DTOs.Disciplinas;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisciplinasController : ControllerBase
{
    private readonly IDisciplinaService _service;

    public DisciplinasController(IDisciplinaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var disciplinas = await _service.GetAllAsync();
        return Ok(disciplinas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var disciplina = await _service.GetByIdAsync(id);
        if (disciplina is null)
            return NotFound(new { message = "Disciplina não encontrada." });

        return Ok(disciplina);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DisciplinaRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Disciplina!.Id }, result.Disciplina);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] DisciplinaRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.Disciplina);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(DisciplinaCommandResult result)
    {
        return result.Error switch
        {
            DisciplinaResultError.NotFound => NotFound(new { message = result.Message }),
            DisciplinaResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
