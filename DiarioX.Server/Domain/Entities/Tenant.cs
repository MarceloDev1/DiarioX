namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Instituição ou mantenedora (rede) que contrata o sistema. Agrupa uma ou mais escolas
/// e isola todos os dados cadastrados por ela.
/// </summary>
public class Tenant
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";

    public const string SituacaoFinanceiraRegular = "REGULAR";
    public const string SituacaoFinanceiraEmAtraso = "EM_ATRASO";
    public const string SituacaoFinanceiraSomenteLeitura = "SOMENTE_LEITURA";

    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    // Subdomínio de acesso da instituição (ex.: "colegio-x" em colegio-x.diariox.online).
    public string Slug { get; set; } = string.Empty;

    public string Status { get; set; } = StatusAtivo;

    /// <summary>
    /// Situação das faturas do Diário X, mantida pela rotina de faturamento. Em SOMENTE_LEITURA a
    /// instituição consulta tudo, mas só grava chamadas.
    /// </summary>
    public string SituacaoFinanceira { get; set; } = SituacaoFinanceiraRegular;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
