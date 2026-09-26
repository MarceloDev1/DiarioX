namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Assinatura de uma instituição (entidade global, uma por instituição). Os dados do tomador
/// identificam quem paga: vão para o cliente no Asaas e para a NFS-e da plataforma.
/// </summary>
public class Assinatura
{
    public const string SituacaoTeste = "TESTE";
    public const string SituacaoAtiva = "ATIVA";
    public const string SituacaoCancelada = "CANCELADA";

    public const int DiaVencimentoMaximo = 28;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int PlanoId { get; set; }
    public PlanoAssinatura Plano { get; set; } = null!;
    public string Situacao { get; set; } = SituacaoAtiva;
    public DateOnly DataInicio { get; set; }

    /// <summary>Fim do período de teste; vencimentos até esta data não geram fatura.</summary>
    public DateOnly? TesteAte { get; set; }

    /// <summary>Dia do mês do vencimento (1 a 28, para existir em todos os meses).</summary>
    public int DiaVencimento { get; set; } = 10;

    public decimal DescontoPercentual { get; set; }

    // Tomador (cobrança e nota fiscal)
    public string RazaoSocial { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }

    public string? AsaasClienteId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
