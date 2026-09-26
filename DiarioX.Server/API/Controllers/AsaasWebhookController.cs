using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiarioX.Server.Application.Faturamento;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DiarioX.Server.API.Controllers;

/// <summary>
/// Webhook do Asaas (conta da plataforma). Configure no Asaas a URL /api/webhooks/asaas com o
/// mesmo token de Asaas:WebhookToken e os eventos de cobrança e de nota fiscal.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/webhooks/asaas")]
public class AsaasWebhookController : ControllerBase
{
    private readonly IFaturamentoService _faturamentoService;
    private readonly AsaasOptions _options;
    private readonly ILogger<AsaasWebhookController> _logger;

    public AsaasWebhookController(
        IFaturamentoService faturamentoService,
        IOptions<AsaasOptions> options,
        ILogger<AsaasWebhookController> logger)
    {
        _faturamentoService = faturamentoService;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receber([FromBody] JsonElement corpo)
    {
        if (string.IsNullOrEmpty(_options.WebhookToken))
        {
            _logger.LogWarning("Webhook do Asaas recebido, mas Asaas:WebhookToken não está configurado.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Webhook não configurado." });
        }

        if (!TokenValido(Request.Headers["asaas-access-token"].ToString()))
            return Unauthorized();

        var evento = LerEvento(corpo);
        if (evento is null)
            return Ok(); // formato desconhecido: nada a fazer, sem travar a fila do Asaas

        // Erros inesperados devolvem 500 para o Asaas reenviar; os eventos são idempotentes.
        await _faturamentoService.ProcessarEventoAsync(evento, DateOnly.FromDateTime(DateTime.Today));
        return Ok();
    }

    private bool TokenValido(string recebido)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(_options.WebhookToken!));

    private static AsaasEvento? LerEvento(JsonElement corpo)
    {
        if (corpo.ValueKind != JsonValueKind.Object || Texto(corpo, "event") is not string evento)
            return null;

        AsaasEventoCobranca? cobranca = null;
        if (corpo.TryGetProperty("payment", out var p) && p.ValueKind == JsonValueKind.Object && Texto(p, "id") is string pagamentoId)
        {
            cobranca = new AsaasEventoCobranca(
                pagamentoId,
                Texto(p, "status"),
                p.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null,
                Texto(p, "billingType"),
                Data(Texto(p, "clientPaymentDate") ?? Texto(p, "paymentDate")),
                Texto(p, "externalReference"));
        }

        AsaasEventoNotaFiscal? nota = null;
        if (corpo.TryGetProperty("invoice", out var i) && i.ValueKind == JsonValueKind.Object && Texto(i, "id") is string notaId)
        {
            nota = new AsaasEventoNotaFiscal(
                notaId,
                Texto(i, "status"),
                Texto(i, "number"),
                Texto(i, "pdfUrl"),
                Texto(i, "payment"),
                Texto(i, "statusDescription"));
        }

        return new AsaasEvento(evento, cobranca, nota);
    }

    private static string? Texto(JsonElement objeto, string propriedade)
        => objeto.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;

    private static DateOnly? Data(string? valor)
        => DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : null;
}
