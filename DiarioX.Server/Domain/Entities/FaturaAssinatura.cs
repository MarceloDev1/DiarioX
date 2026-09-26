namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Fatura mensal da assinatura (entidade global). Guarda o cálculo usado (alunos ativos e valores
/// do plano na data de geração), a cobrança no Asaas e a NFS-e emitida pela plataforma.
/// </summary>
public class FaturaAssinatura
{
    public const string SituacaoPendente = "PENDENTE";
    public const string SituacaoPaga = "PAGA";
    public const string SituacaoVencida = "VENCIDA";
    public const string SituacaoCancelada = "CANCELADA";
    public const string SituacaoEstornada = "ESTORNADA";

    public const string NotaAgendada = "AGENDADA";
    public const string NotaEmitida = "EMITIDA";
    public const string NotaCancelada = "CANCELADA";
    public const string NotaErro = "ERRO";

    public int Id { get; set; }
    public int AssinaturaId { get; set; }
    public Assinatura Assinatura { get; set; } = null!;
    public int TenantId { get; set; }

    /// <summary>Primeiro dia do mês de referência (mês do vencimento).</summary>
    public DateOnly Competencia { get; set; }
    public DateOnly Vencimento { get; set; }

    // Memória de cálculo
    public int AlunosAtivos { get; set; }
    public decimal ValorFixo { get; set; }
    public decimal ValorPorAluno { get; set; }
    public decimal ValorMinimo { get; set; }
    public decimal ValorCalculado { get; set; }
    public decimal DescontoPercentual { get; set; }
    public decimal Valor { get; set; }

    public string Situacao { get; set; } = SituacaoPendente;

    // Cobrança no Asaas
    public string? AsaasCobrancaId { get; set; }
    public string? LinkPagamento { get; set; }
    public DateOnly? PagaEm { get; set; }
    public decimal? ValorPago { get; set; }
    public string? FormaPagamento { get; set; }

    // NFS-e da plataforma
    public string? NotaFiscalId { get; set; }
    public string? NotaFiscalSituacao { get; set; }
    public string? NotaFiscalNumero { get; set; }
    public string? NotaFiscalPdfUrl { get; set; }
    public string? NotaFiscalErro { get; set; }

    /// <summary>Avisos da geração (ex.: cobrança não enviada ao Asaas).</summary>
    public string? Observacao { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool EmAberto => Situacao is SituacaoPendente or SituacaoVencida;
}
