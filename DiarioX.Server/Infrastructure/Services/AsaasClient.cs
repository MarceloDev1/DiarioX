using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiarioX.Server.Application.Faturamento;
using Microsoft.Extensions.Options;

namespace DiarioX.Server.Infrastructure.Services;

/// <summary>
/// Cliente da API v3 do Asaas usando a conta da plataforma (Asaas:ApiKey).
/// Referência: https://docs.asaas.com/reference
/// </summary>
public class AsaasClient : IAsaasClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly AsaasOptions _options;

    public AsaasClient(HttpClient http, IOptions<AsaasOptions> options)
    {
        _http = http;
        _options = options.Value;

        _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("DiarioX/1.0");
        if (Configurado)
            _http.DefaultRequestHeaders.Add("access_token", _options.ApiKey);
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string> SalvarClienteAsync(string? clienteId, AsaasCliente cliente, CancellationToken ct = default)
    {
        var body = new
        {
            name = cliente.Nome,
            cpfCnpj = cliente.CpfCnpj,
            email = cliente.Email,
            mobilePhone = cliente.Telefone,
            postalCode = cliente.Cep,
            address = cliente.Endereco,
            addressNumber = cliente.Numero,
            complement = cliente.Complemento,
            province = cliente.Bairro,
            externalReference = cliente.ReferenciaExterna,
        };

        var resposta = clienteId is null
            ? await EnviarAsync<AsaasIdResponse>(HttpMethod.Post, "customers", body, ct)
            : await EnviarAsync<AsaasIdResponse>(HttpMethod.Put, $"customers/{clienteId}", body, ct);
        return resposta.Id;
    }

    public async Task<AsaasCobranca> CriarCobrancaAsync(AsaasNovaCobranca cobranca, CancellationToken ct = default)
    {
        // UNDEFINED: o cliente escolhe boleto, PIX ou cartão na página da fatura.
        var body = new
        {
            customer = cobranca.ClienteId,
            billingType = "UNDEFINED",
            value = cobranca.Valor,
            dueDate = cobranca.Vencimento.ToString("yyyy-MM-dd"),
            description = cobranca.Descricao,
            externalReference = cobranca.ReferenciaExterna,
            fine = cobranca.MultaPercentual > 0 ? new { value = cobranca.MultaPercentual, type = "PERCENTAGE" } : null,
            interest = cobranca.JurosMensalPercentual > 0 ? new { value = cobranca.JurosMensalPercentual } : null,
        };

        var resposta = await EnviarAsync<AsaasPaymentResponse>(HttpMethod.Post, "payments", body, ct);
        return new AsaasCobranca(resposta.Id, resposta.Status ?? "PENDING", resposta.InvoiceUrl);
    }

    public async Task CancelarCobrancaAsync(string cobrancaId, CancellationToken ct = default)
        => await EnviarAsync<JsonElement>(HttpMethod.Delete, $"payments/{cobrancaId}", null, ct);

    public async Task ConfirmarRecebimentoEmDinheiroAsync(string cobrancaId, DateOnly data, decimal valor, CancellationToken ct = default)
    {
        var body = new { paymentDate = data.ToString("yyyy-MM-dd"), value = valor, notifyCustomer = false };
        await EnviarAsync<JsonElement>(HttpMethod.Post, $"payments/{cobrancaId}/receiveInCash", body, ct);
    }

    public async Task<AsaasNotaFiscal> AgendarNotaFiscalAsync(AsaasNovaNotaFiscal nota, CancellationToken ct = default)
    {
        var body = new
        {
            payment = nota.CobrancaId,
            serviceDescription = nota.DescricaoServico,
            observations = string.Empty,
            externalReference = nota.ReferenciaExterna,
            value = nota.Valor,
            deductions = 0m,
            effectiveDate = nota.DataEmissao.ToString("yyyy-MM-dd"),
            municipalServiceId = Opcional(nota.MunicipalServiceId),
            municipalServiceCode = Opcional(nota.MunicipalServiceCode),
            municipalServiceName = nota.MunicipalServiceName ?? string.Empty,
            taxes = new
            {
                retainIss = nota.RetemIss,
                iss = nota.AliquotaIss,
                pis = 0m,
                cofins = 0m,
                csll = 0m,
                inss = 0m,
                ir = 0m,
            },
        };

        var resposta = await EnviarAsync<AsaasIdStatusResponse>(HttpMethod.Post, "invoices", body, ct);
        return new AsaasNotaFiscal(resposta.Id, resposta.Status ?? "SCHEDULED");
    }

    private async Task<T> EnviarAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        if (!Configurado)
            throw new AsaasException("Integração com o Asaas não configurada (Asaas:ApiKey).");

        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new AsaasException("Não foi possível comunicar com o Asaas. Tente novamente em instantes.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new AsaasException(await LerErroAsync(response, ct));

            return (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
        }
    }

    // O Asaas responde {"errors":[{"code":"...","description":"..."}]}.
    private static async Task<string> LerErroAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var erro = await response.Content.ReadFromJsonAsync<AsaasErrorResponse>(Json, ct);
            var descricoes = erro?.Errors?.Select(e => e.Description).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
            if (descricoes is { Count: > 0 })
                return $"Asaas: {string.Join(" ", descricoes)}";
        }
        catch (JsonException)
        {
            // corpo fora do padrão: cai na mensagem genérica
        }

        return $"Asaas respondeu {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;

    private record AsaasIdResponse(string Id);
    private record AsaasIdStatusResponse(string Id, string? Status);
    private record AsaasPaymentResponse(string Id, string? Status, string? InvoiceUrl);
    private record AsaasErrorResponse(List<AsaasErrorItem>? Errors);
    private record AsaasErrorItem(string? Code, string? Description);
}
