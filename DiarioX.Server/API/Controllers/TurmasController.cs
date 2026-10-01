using DiarioX.Server.Application.Auth;
using DiarioX.Server.Infrastructure.Authorization;
using DiarioX.Server.Application.DTOs.Turmas;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurmasController : ControllerBase
{
    private readonly ITurmaService _service;
    private readonly IRemanejamentoAlunoService _remanejamentoAlunoService;

    public TurmasController(ITurmaService service, IRemanejamentoAlunoService remanejamentoAlunoService)
    {
        _service = service;
        _remanejamentoAlunoService = remanejamentoAlunoService;
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

    /// <summary>
    /// Vagas disponíveis para uma enturmação iniciada na data informada (padrão: hoje).
    /// </summary>
    [HttpGet("{id:int}/vagas")]
    public async Task<IActionResult> GetVagas([FromRoute] int id, [FromQuery] DateOnly? data)
    {
        var vagas = await _remanejamentoAlunoService.GetVagasTurmaAsync(id, data ?? DateOnly.FromDateTime(DateTime.Today));
        if (vagas is null)
            return NotFound(new { message = "Turma não encontrada." });

        return Ok(vagas);
    }

    /// <summary>
    /// Alunos com enturmação ativa na turma, em ordem alfabética.
    /// </summary>
    [Permissao(Permissoes.Alunos.Visualizar)]
    [HttpGet("{id:int}/alunos")]
    public async Task<IActionResult> GetAlunos([FromRoute] int id)
    {
        var alunos = await _remanejamentoAlunoService.GetAlunosEnturmadosAsync(id);
        if (alunos is null)
            return NotFound(new { message = "Turma não encontrada." });

        return Ok(alunos);
    }

    [Permissao(Permissoes.Turmas.Criar)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TurmaRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Turma!.Id }, result.Turma);
    }

    [Permissao(Permissoes.Turmas.Editar)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TurmaRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.Turma);
    }

    [Permissao(Permissoes.Turmas.Editar)]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] TurmaStatusRequest request)
    {
        var result = await _service.UpdateStatusAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message, turma = result.Turma });
    }

    [Permissao(Permissoes.Turmas.Excluir)]
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