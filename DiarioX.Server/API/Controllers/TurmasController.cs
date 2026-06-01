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