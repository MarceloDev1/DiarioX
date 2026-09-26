using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Faturamento;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

/// <summary>Faturamento da plataforma: exclusivo do Administrador global.</summary>
[ApiController]
[Authorize(Policy = AppPolicies.GlobalAdmin)]
[Route("api/[controller]")]
public class FaturamentoController : ControllerBase
{
    private readonly IFaturamentoService _faturamentoService;

    public FaturamentoController(IFaturamentoService faturamentoService)
    {
        _faturamentoService = faturamentoService;
    }

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Today);

    [HttpGet("painel")]
    public async Task<IActionResult> GetPainel()
        => Ok(await _faturamentoService.ObterPainelAsync(Hoje));

    [HttpGet("planos")]
    public async Task<IActionResult> GetPlanos()
        => Ok(await _faturamentoService.ListarPlanosAsync());

    [HttpPost("planos")]
    public async Task<IActionResult> CreatePlano([FromBody] PlanoRequest request)
        => FromResult(await _faturamentoService.SalvarPlanoAsync(null, request));

    [HttpPut("planos/{id:int}")]
    public async Task<IActionResult> UpdatePlano([FromRoute] int id, [FromBody] PlanoRequest request)
        => FromResult(await _faturamentoService.SalvarPlanoAsync(id, request));

    [HttpGet("assinaturas")]
    public async Task<IActionResult> GetAssinaturas()
        => Ok(await _faturamentoService.ListarAssinaturasAsync(Hoje));

    [HttpGet("assinaturas/{tenantId:int}")]
    public async Task<IActionResult> GetAssinatura([FromRoute] int tenantId)
        => FromResult(await _faturamentoService.ObterAssinaturaAsync(tenantId));

    [HttpPut("assinaturas/{tenantId:int}")]
    public async Task<IActionResult> SaveAssinatura([FromRoute] int tenantId, [FromBody] AssinaturaRequest request)
        => FromResult(await _faturamentoService.SalvarAssinaturaAsync(tenantId, request));

    [HttpPost("assinaturas/{tenantId:int}/faturas")]
    public async Task<IActionResult> GerarFatura([FromRoute] int tenantId, [FromBody] GerarFaturaRequest? request)
        => FromResult(await _faturamentoService.GerarFaturaAsync(tenantId, request ?? new GerarFaturaRequest(), Hoje));

    [HttpGet("faturas")]
    public async Task<IActionResult> GetFaturas([FromQuery] int? tenantId, [FromQuery] string? situacao)
        => Ok(await _faturamentoService.ListarFaturasAsync(tenantId, situacao, Hoje));

    [HttpPost("faturas/{id:int}/pagamento-manual")]
    public async Task<IActionResult> RegistrarPagamento([FromRoute] int id, [FromBody] PagamentoManualRequest request)
        => FromResult(await _faturamentoService.RegistrarPagamentoManualAsync(id, request, Hoje));

    [HttpPost("faturas/{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar([FromRoute] int id)
        => FromResult(await _faturamentoService.CancelarFaturaAsync(id, Hoje));

    [HttpPost("faturas/{id:int}/nota-fiscal")]
    public async Task<IActionResult> EmitirNotaFiscal([FromRoute] int id)
        => FromResult(await _faturamentoService.EmitirNotaFiscalAsync(id, Hoje));

    /// <summary>Executa a rotina diária na hora (geração, reenvio e régua de atraso).</summary>
    [HttpPost("rotina")]
    public async Task<IActionResult> ExecutarRotina(CancellationToken ct)
        => Ok(await _faturamentoService.ExecutarRotinaAsync(Hoje, ct));

    private IActionResult FromResult<T>(FaturamentoResult<T> result)
    {
        if (result.Success)
            return Ok(new { message = result.Message, data = result.Value });

        return result.Error switch
        {
            FaturamentoErro.NotFound => NotFound(new { message = result.Message }),
            FaturamentoErro.Conflict => Conflict(new { message = result.Message }),
            FaturamentoErro.Integracao => StatusCode(StatusCodes.Status502BadGateway, new { message = result.Message }),
            _ => BadRequest(new { message = result.Message }),
        };
    }
}
