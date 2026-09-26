using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Relatorios;
using DiarioX.Server.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Permissao(Permissoes.Relatorios.Visualizar)]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _relatorioService;

    public RelatoriosController(IRelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    /// <summary>Catálogo dos relatórios que o usuário pode gerar.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        return Ok(await _relatorioService.ListarAsync(usuario));
    }

    /// <summary>Escolas, anos letivos e turmas para os filtros, restritos às escolas do usuário.</summary>
    [HttpGet("opcoes")]
    public async Task<IActionResult> Opcoes()
    {
        return Ok(await _relatorioService.ObterOpcoesAsync());
    }

    /// <summary>Gera o relatório: JSON para a tela ou arquivo com formato=xlsx|pdf.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Gerar([FromRoute] string id, [FromQuery] RelatorioFiltros filtros, [FromQuery] string? formato)
    {
        if (!User.TryGetUsuarioAtual(out var usuario))
            return Unauthorized();

        var resultado = await _relatorioService.GerarAsync(usuario, id, filtros);
        if (!resultado.Success)
            return MapError(resultado.Error, resultado.Message);

        if (string.IsNullOrWhiteSpace(formato) || string.Equals(formato, "json", StringComparison.OrdinalIgnoreCase))
            return Ok(resultado.Value);

        var arquivo = _relatorioService.Exportar(resultado.Value!, formato);
        if (!arquivo.Success)
            return MapError(arquivo.Error, arquivo.Message);

        return File(arquivo.Value!.Conteudo, arquivo.Value.ContentType, arquivo.Value.NomeArquivo);
    }

    private IActionResult MapError(RelatorioErro error, string message)
    {
        return error switch
        {
            RelatorioErro.NotFound => NotFound(new { message }),
            RelatorioErro.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message }),
            _ => BadRequest(new { message })
        };
    }
}
