namespace DiarioX.Server.Application.Faturamento;

/// <summary>Cliente da API v3 do Asaas (cobranças, clientes e notas fiscais de serviço).</summary>
public interface IAsaasClient
{
    /// <summary>Indica se há chave de API configurada. Sem ela, as faturas ficam só no sistema.</summary>
    bool Configurado { get; }

    /// <summary>Cria o cliente (clienteId nulo) ou atualiza os dados de um existente; devolve o id.</summary>
    Task<string> SalvarClienteAsync(string? clienteId, AsaasCliente cliente, CancellationToken ct = default);

    Task<AsaasCobranca> CriarCobrancaAsync(AsaasNovaCobranca cobranca, CancellationToken ct = default);
    Task CancelarCobrancaAsync(string cobrancaId, CancellationToken ct = default);
    Task ConfirmarRecebimentoEmDinheiroAsync(string cobrancaId, DateOnly data, decimal valor, CancellationToken ct = default);

    Task<AsaasNotaFiscal> AgendarNotaFiscalAsync(AsaasNovaNotaFiscal nota, CancellationToken ct = default);
}

public record AsaasCliente(
    string Nome,
    string CpfCnpj,
    string Email,
    string? Telefone,
    string? Cep,
    string? Endereco,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string ReferenciaExterna
);

public record AsaasNovaCobranca(
    string ClienteId,
    decimal Valor,
    DateOnly Vencimento,
    string Descricao,
    string ReferenciaExterna,
    decimal MultaPercentual,
    decimal JurosMensalPercentual
);

public record AsaasCobranca(string Id, string Status, string? LinkPagamento);

public record AsaasNovaNotaFiscal(
    string CobrancaId,
    decimal Valor,
    string DescricaoServico,
    string ReferenciaExterna,
    DateOnly DataEmissao,
    string? MunicipalServiceId,
    string? MunicipalServiceCode,
    string? MunicipalServiceName,
    decimal AliquotaIss,
    bool RetemIss
);

public record AsaasNotaFiscal(string Id, string Status);

public class AsaasException : Exception
{
    public AsaasException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

// ---------- Webhook ----------

/// <summary>Evento recebido do webhook do Asaas (apenas os campos usados).</summary>
public record AsaasEvento(string Evento, AsaasEventoCobranca? Cobranca, AsaasEventoNotaFiscal? NotaFiscal);

public record AsaasEventoCobranca(
    string Id,
    string? Status,
    decimal? Valor,
    string? FormaPagamento,
    DateOnly? DataPagamento,
    string? ReferenciaExterna
);

public record AsaasEventoNotaFiscal(
    string Id,
    string? Status,
    string? Numero,
    string? PdfUrl,
    string? CobrancaId,
    string? Descricao
);
