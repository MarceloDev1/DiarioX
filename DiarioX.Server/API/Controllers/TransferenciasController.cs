using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Alunos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

/// <summary>
/// Transferência externa de alunos (RF014) e emissão da Declaração de Transferência.
/// </summary>
[ApiController]
[Route("api")]
public class TransferenciasController : ControllerBase
{
    private readonly ITransferenciaAlunoService _service;

    public TransferenciasController(ITransferenciaAlunoService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista os alunos transferidos, com escola de origem, modalidade, etapa, turma e turno.
    /// </summary>
    [Permissao(Permissoes.Alunos.Visualizar)]
    [HttpGet("transferencias")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        return Ok(await _service.ListAsync());
    }

    /// <summary>Transferências do aluno, da mais recente para a mais antiga.</summary>
    [Permissao(Permissoes.Alunos.Visualizar)]
    [HttpGet("alunos/{alunoId:int}/transferencias")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAluno([FromRoute] int alunoId)
    {
        return Ok(await _service.GetByAlunoIdAsync(alunoId));
    }

    /// <summary>
    /// Registra a saída definitiva do aluno: status TRANSFERIDO, enturmação encerrada na data do
    /// desligamento e vaga liberada. A resposta traz a transferência, para emitir a declaração.
    /// </summary>
    [Permissao(Permissoes.Alunos.Editar)]
    [HttpPost("alunos/{alunoId:int}/transferencias")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transferir([FromRoute] int alunoId, [FromBody] TransferenciaRequest request)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var result = await _service.TransferirAsync(usuario, alunoId, request);
        if (!result.Success)
        {
            var body = new { message = result.Message };
            return result.Error switch
            {
                AlunoResultError.NotFound => NotFound(body),
                AlunoResultError.Conflict => Conflict(body),
                _ => BadRequest(body)
            };
        }

        return Ok(new { message = result.Message, transferencia = result.Transferencia });
    }

    /// <summary>Declaração de Transferência em PDF.</summary>
    [Permissao(Permissoes.Alunos.Visualizar)]
    [HttpGet("transferencias/{id:int}/declaracao")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeclaracao([FromRoute] int id)
    {
        var arquivo = await _service.GerarDeclaracaoAsync(id);
        if (arquivo is null)
            return NotFound(new { message = "Transferência não encontrada." });

        return File(arquivo.Conteudo, "application/pdf", arquivo.NomeArquivo);
    }
}
