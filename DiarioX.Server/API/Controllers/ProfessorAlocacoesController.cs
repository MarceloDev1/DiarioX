using DiarioX.Server.Application.DTOs.Alocacoes;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Authorize]
[Route("api/professor-alocacoes")]
public class ProfessorAlocacoesController : ControllerBase
{
    private readonly IProfessorAlocacaoService _service;

    public ProfessorAlocacoesController(IProfessorAlocacaoService service)
    {
        _service = service;
    }

    [HttpGet("professor/{professorId:int}")]
    public async Task<IActionResult> GetByProfessor(int professorId)
        => Ok(await _service.GetByProfessorAsync(professorId));

    [HttpGet("professor/{professorId:int}/disponiveis")]
    public async Task<IActionResult> GetDisponiveis(int professorId)
        => Ok(await _service.GetDisponiveisAsync(professorId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProfessorAlocacaoRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message, alocacoes = result.Alocacoes });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(ProfessorAlocacaoCommandResult result)
        => result.Error switch
        {
            ProfessorAlocacaoResultError.NotFound => NotFound(new { message = result.Message }),
            ProfessorAlocacaoResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
}