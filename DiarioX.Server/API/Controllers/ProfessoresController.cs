using DiarioX.Server.Application.DTOs.Professores;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfessoresController : ControllerBase
{
    private readonly IProfessorService _professorService;

    public ProfessoresController(IProfessorService professorService)
    {
        _professorService = professorService;
    }

    /// <summary>
    /// Lista todos os professores cadastrados.
    /// </summary>
    /// <returns>Lista de professores</returns>
    /// <response code="200">Professores listados com sucesso</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var professors = await _professorService.GetAllAsync();
        return Ok(professors);
    }

    /// <summary>
    /// Obtém um professor específico pelo ID.
    /// </summary>
    /// <param name="id">ID do professor</param>
    /// <returns>Dados do professor</returns>
    /// <response code="200">Professor encontrado</response>
    /// <response code="404">Professor não encontrado</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var result = await _professorService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(new { message = result.Message });

        return Ok(result.Professor);
    }

    /// <summary>
    /// Lista os professores de uma escola específica.
    /// </summary>
    /// <param name="escolaId">ID da escola</param>
    /// <returns>Lista de professores da escola</returns>
    /// <response code="200">Professores listados com sucesso</response>
    [HttpGet("escola/{escolaId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEscolaId([FromRoute] int escolaId)
    {
        var professors = await _professorService.GetByEscolaIdAsync(escolaId);
        return Ok(professors);
    }

    /// <summary>
    /// Cria um novo professor.
    /// </summary>
    /// <remarks>
    /// Regras de negócio:
    /// - CPF e Matrícula devem ser únicos
    /// - Ao menos uma disciplina deve ser selecionada
    /// - Um usuário é criado automaticamente com perfil "Professor"
    /// - Um email de boas-vindas é enviado
    /// </remarks>
    /// <param name="request">Dados do professor a ser criado</param>
    /// <returns>Dados do professor criado</returns>
    /// <response code="201">Professor criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="409">CPF ou Matrícula já existe</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ProfessorRequest request)
    {
        var result = await _professorService.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Professor!.Id }, result.Professor);
    }

    /// <summary>
    /// Atualiza os dados de um professor.
    /// </summary>
    /// <param name="id">ID do professor a ser atualizado</param>
    /// <param name="request">Novos dados do professor</param>
    /// <returns>Dados do professor atualizado</returns>
    /// <response code="200">Professor atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Professor não encontrado</response>
    /// <response code="409">CPF ou Matrícula já existe</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProfessorRequest request)
    {
        var result = await _professorService.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.Professor);
    }

    /// <summary>
    /// Deleta um professor do sistema.
    /// </summary>
    /// <param name="id">ID do professor a ser deletado</param>
    /// <returns>Mensagem de sucesso</returns>
    /// <response code="200">Professor deletado com sucesso</response>
    /// <response code="404">Professor não encontrado</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var result = await _professorService.DeleteAsync(id);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message });
    }

    private IActionResult MapError(ProfessorCommandResult result)
    {
        return result.Error switch
        {
            ProfessorResultError.NotFound => NotFound(new { message = result.Message }),
            ProfessorResultError.Conflict => Conflict(new { message = result.Message }),
            ProfessorResultError.DependencyNotFound => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
