using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

/// <summary>
/// Gerencia o cadastro de alunos.
/// </summary>
/// <remarks>
/// Pré-condição do RF: usuário autenticado com perfil de secretaria ou administrativo
/// (Administrador, Gerência, Diretor ou Secretário). O sistema ainda não aplica autorização
/// por perfil em nenhum controller, então essa checagem não é aplicada aqui (ver decisão de escopo).
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AlunosController : ControllerBase
{
    private readonly IAlunoService _alunoService;

    public AlunosController(IAlunoService alunoService)
    {
        _alunoService = alunoService;
    }

    /// <summary>
    /// Lista todos os alunos cadastrados.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var alunos = await _alunoService.GetAllAsync();
        return Ok(alunos);
    }

    /// <summary>
    /// Obtém um aluno específico pelo ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var result = await _alunoService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(new { message = result.Message });

        return Ok(result.Aluno);
    }

    /// <summary>
    /// Cadastra um novo aluno.
    /// </summary>
    /// <remarks>
    /// Regras de negócio:
    /// - RN01: CPF do aluno (quando informado) é único; sem CPF, a chave de unicidade é Nome + Data de Nascimento + Responsável 1.
    /// - RN02: a matrícula é gerada automaticamente (ano + sequencial) e nunca pode ser alterada.
    /// - RN03: alunos com 18 anos ou mais precisam de CPF próprio.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] AlunoRequest request)
    {
        var result = await _alunoService.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Aluno!.Id }, result.Aluno);
    }

    /// <summary>
    /// Atualiza os dados de um aluno.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AlunoRequest request)
    {
        var result = await _alunoService.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.Aluno);
    }

    /// <summary>
    /// Remove um aluno do sistema.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _alunoService.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(AlunoCommandResult result)
    {
        return result.Error switch
        {
            AlunoResultError.NotFound => NotFound(new { message = result.Message }),
            AlunoResultError.Conflict => Conflict(new { message = result.Message }),
            AlunoResultError.DependencyNotFound => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
