using DiarioX.Server.Application.DTOs.ModalidadesEnsino;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModalidadesEnsinoController : ControllerBase
{
    private readonly IModalidadeEnsinoService _service;

    public ModalidadesEnsinoController(IModalidadeEnsinoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var modalidades = await _service.GetAllAsync();
        return Ok(modalidades);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var modalidade = await _service.GetByIdAsync(id);
        if (modalidade is null)
            return NotFound(new { message = "Modalidade de ensino nao encontrada." });

        return Ok(modalidade);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ModalidadeEnsinoRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.ModalidadeEnsino!.Id }, result.ModalidadeEnsino);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ModalidadeEnsinoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.ModalidadeEnsino);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(ModalidadeEnsinoCommandResult result)
    {
        return result.Error switch
        {
            ModalidadeEnsinoResultError.NotFound => NotFound(new { message = result.Message }),
            ModalidadeEnsinoResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
