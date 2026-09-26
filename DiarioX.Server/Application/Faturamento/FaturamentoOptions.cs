namespace DiarioX.Server.Application.Faturamento;

/// <summary>Seção "Asaas" do appsettings. A chave de API fica fora do repositório (user-secrets ou variável de ambiente).</summary>
public class AsaasOptions
{
    public const string Secao = "Asaas";

    /// <summary>Sandbox por padrão; em produção use https://api.asaas.com/v3.</summary>
    public string BaseUrl { get; set; } = "https://api-sandbox.asaas.com/v3";

    public string? ApiKey { get; set; }

    /// <summary>Token informado na configuração do webhook no Asaas (enviado no header asaas-access-token).</summary>
    public string? WebhookToken { get; set; }
}

/// <summary>Seção "Faturamento" do appsettings: regras de cobrança da plataforma.</summary>
public class FaturamentoOptions
{
    public const string Secao = "Faturamento";

    /// <summary>Liga a rotina automática (geração de faturas e régua de atraso).</summary>
    public bool RotinaHabilitada { get; set; } = true;
    public int IntervaloRotinaMinutos { get; set; } = 360;

    /// <summary>Quantos dias antes do vencimento a fatura é gerada e enviada.</summary>
    public int DiasAntecedenciaFatura { get; set; } = 10;

    /// <summary>Dias de atraso da fatura mais antiga para a instituição entrar em somente leitura.</summary>
    public int DiasAtrasoSomenteLeitura { get; set; } = 10;

    public decimal MultaPercentual { get; set; } = 2m;
    public decimal JurosMensalPercentual { get; set; } = 1m;

    public NotaFiscalOptions NotaFiscal { get; set; } = new();
}

/// <summary>
/// NFS-e emitida pela plataforma quando a fatura é paga. Os dados fiscais (inscrição municipal,
/// certificado) são configurados na conta Asaas; aqui ficam o serviço e os tributos da nota.
/// </summary>
public class NotaFiscalOptions
{
    public bool Habilitada { get; set; }

    /// <summary>Id do serviço municipal no Asaas (GET /invoices/municipalServices), quando a prefeitura exige.</summary>
    public string? MunicipalServiceId { get; set; }
    public string? MunicipalServiceCode { get; set; }
    public string? MunicipalServiceName { get; set; }

    /// <summary>Texto da nota; {competencia} e {alunos} são substituídos.</summary>
    public string DescricaoServico { get; set; } =
        "Licença de uso do sistema Diário X - competência {competencia} ({alunos} alunos ativos).";

    public decimal AliquotaIss { get; set; }
    public bool RetemIss { get; set; }
}
