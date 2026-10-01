using DiarioX.Server.Application.Auth;
using DiarioX.Server.Infrastructure.Authorization;
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
    private readonly IRemanejamentoAlunoService _remanejamentoAlunoService;

    public AlunosController(IAlunoService alunoService, IRemanejamentoAlunoService remanejamentoAlunoService)
    {
        _alunoService = alunoService;
        _remanejamentoAlunoService = remanejamentoAlunoService;
    }

    /// <summary>
    /// Lista todos os alunos cadastrados.
    /// </summary>
    [Permissao(Permissoes.Alunos.Visualizar)]
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
    [Permissao(Permissoes.Alunos.Visualizar)]
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

    [Permissao(Permissoes.Alunos.Visualizar)]
    [HttpGet("{id:int}/enturmacao-ativa")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnturmacaoAtiva([FromRoute] int id)
    {
        var enturmacao = await _remanejamentoAlunoService.GetEnturmacaoAtivaAsync(id);
        return enturmacao is null
            ? NotFound(new { message = "O aluno não possui enturmação ativa." })
            : Ok(enturmacao);
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
    [Permissao(Permissoes.Alunos.Criar)]
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
    [Permissao(Permissoes.Alunos.Editar)]
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
    /// Inativa ou reativa um aluno.
    /// </summary>
    /// <remarks>
    /// Ao reativar, o status volta para ATIVO se houver enturmação ativa,
    /// ou ATIVO_AGUARDANDO_ENTURMACAO caso contrário.
    /// </remarks>
    [Permissao(Permissoes.Alunos.Editar)]
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] AlunoStatusRequest request)
    {
        var result = await _alunoService.UpdateStatusAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(new { message = result.Message, aluno = result.Aluno });
    }

    /// <summary>
    /// Remove um aluno do sistema.
    /// </summary>
    [Permissao(Permissoes.Alunos.Excluir)]
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

    [Permissao(Permissoes.Alunos.Editar)]
    [HttpPost("{id:int}/remanejamentos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remanejar([FromRoute] int id, [FromBody] RemanejamentoAlunoRequest request)
    {
        var result = await _remanejamentoAlunoService.RemanejarAsync(id, request);
        if (!result.Success)
            return result.Error switch
            {
                AlunoResultError.NotFound => NotFound(new { message = result.Message }),
                AlunoResultError.Conflict => Conflict(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message })
            };

        return Ok(new { message = result.Message });
    }

    [Permissao(Permissoes.Alunos.Editar)]
    [HttpPost("{id:int}/enturmacoes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enturmar([FromRoute] int id, [FromBody] EnturmacaoAlunoRequest request)
    {
        var result = await _remanejamentoAlunoService.EnturmarAsync(id, request);
        if (!result.Success)
            return result.Error switch
            {
                AlunoResultError.NotFound => NotFound(new { message = result.Message }),
                AlunoResultError.Conflict => Conflict(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message })
            };

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Lista os alunos com enturmação ativa, com escola, modalidade, etapa, turma e turno.
    /// </summary>
    [Permissao(Permissoes.Alunos.Visualizar)]
    [HttpGet("enturmacoes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnturmacoesAtivas()
    {
        return Ok(await _remanejamentoAlunoService.GetEnturmacoesAtivasAsync());
    }

    /// <summary>
    /// Enturma vários alunos na mesma turma de uma só vez.
    /// </summary>
    /// <remarks>
    /// Tudo ou nada: se algum aluno não puder ser enturmado ou faltarem vagas, nenhum vínculo é gravado.
    /// Nesse caso, <c>falhas</c> lista os alunos com problema e o motivo de cada um.
    /// </remarks>
    [Permissao(Permissoes.Alunos.Editar)]
    [HttpPost("enturmacoes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnturmarEmLote([FromBody] EnturmacaoLoteRequest request)
    {
        var result = await _remanejamentoAlunoService.EnturmarEmLoteAsync(request);
        if (!result.Success)
        {
            var body = new { message = result.Message, falhas = result.Falhas ?? [] };
            return result.Error switch
            {
                AlunoResultError.NotFound => NotFound(body),
                AlunoResultError.Conflict => Conflict(body),
                _ => BadRequest(body)
            };
        }

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Desenturma um ou mais alunos da mesma turma (RF013).
    /// </summary>
    /// <remarks>
    /// Encerra a enturmação a partir de hoje, liberando as vagas, e muda o status do aluno conforme o motivo:
    /// REESTRUTURACAO_INTERNA, ERRO_MATRICULA_ENTURMACAO e OUTROS voltam para ATIVO_AGUARDANDO_ENTURMACAO;
    /// NAO_COMPARECEU vira NAO_COMPARECEU; FALECIMENTO vira INATIVO_OBITO. A observação é obrigatória para OUTROS.
    /// Tudo ou nada: se algum aluno não estiver enturmado na turma, nada é gravado e <c>falhas</c> aponta quais.
    /// </remarks>
    [Permissao(Permissoes.Alunos.Editar)]
    [HttpPost("desenturmacoes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desenturmar([FromBody] DesenturmacaoRequest request)
    {
        var result = await _remanejamentoAlunoService.DesenturmarAsync(request);
        if (!result.Success)
        {
            var body = new { message = result.Message, falhas = result.Falhas ?? [] };
            return result.Error switch
            {
                AlunoResultError.NotFound => NotFound(body),
                AlunoResultError.Conflict => Conflict(body),
                _ => BadRequest(body)
            };
        }

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
