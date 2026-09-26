namespace DiarioX.Server.Application.DTOs.Faturamento;

// ---------- Planos ----------

public class PlanoRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal ValorFixo { get; set; }
    public decimal ValorPorAluno { get; set; }
    public decimal ValorMinimo { get; set; }
    public bool Ativo { get; set; } = true;
}

public record PlanoResponse(
    int Id,
    string Nome,
    string? Descricao,
    decimal ValorFixo,
    decimal ValorPorAluno,
    decimal ValorMinimo,
    bool Ativo,
    int Assinaturas
);

// ---------- Assinaturas ----------

public class AssinaturaRequest
{
    public int PlanoId { get; set; }
    public string Situacao { get; set; } = "ATIVA";
    public DateOnly DataInicio { get; set; }
    public DateOnly? TesteAte { get; set; }
    public int DiaVencimento { get; set; } = 10;
    public decimal DescontoPercentual { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
}

public record AssinaturaResponse(
    int Id,
    int TenantId,
    string Instituicao,
    int PlanoId,
    string PlanoNome,
    string Situacao,
    DateOnly DataInicio,
    DateOnly? TesteAte,
    int DiaVencimento,
    decimal DescontoPercentual,
    string RazaoSocial,
    string CpfCnpj,
    string Email,
    string? Telefone,
    string? Cep,
    string? Endereco,
    string? Numero,
    string? Complemento,
    string? Bairro,
    bool ClienteNoAsaas
);

/// <summary>Linha da lista de instituições no financeiro (com ou sem assinatura).</summary>
public record AssinaturaResumoResponse(
    int TenantId,
    string Instituicao,
    string Slug,
    string StatusInstituicao,
    string SituacaoFinanceira,
    int? AssinaturaId,
    string? PlanoNome,
    string? Situacao,
    int? DiaVencimento,
    DateOnly? TesteAte,
    int AlunosAtivos,
    decimal? ValorEstimado,
    int FaturasEmAberto,
    decimal ValorEmAberto
);

// ---------- Faturas ----------

public record FaturaResponse(
    int Id,
    int TenantId,
    string Instituicao,
    DateOnly Competencia,
    DateOnly Vencimento,
    int AlunosAtivos,
    decimal ValorFixo,
    decimal ValorPorAluno,
    decimal ValorMinimo,
    decimal ValorCalculado,
    decimal DescontoPercentual,
    decimal Valor,
    string Situacao,
    int DiasEmAtraso,
    string? AsaasCobrancaId,
    string? LinkPagamento,
    DateOnly? PagaEm,
    decimal? ValorPago,
    string? FormaPagamento,
    string? NotaFiscalSituacao,
    string? NotaFiscalNumero,
    string? NotaFiscalPdfUrl,
    string? NotaFiscalErro,
    string? Observacao,
    DateTime CreatedAt
);

public class GerarFaturaRequest
{
    /// <summary>Vencimento da fatura; se omitido, o próximo dia de vencimento da assinatura.</summary>
    public DateOnly? Vencimento { get; set; }
}

public class PagamentoManualRequest
{
    public DateOnly Data { get; set; }
    public decimal? Valor { get; set; }
}

// ---------- Painel e rotina ----------

public record PainelFaturamentoResponse(
    bool AsaasConfigurado,
    bool NotaFiscalHabilitada,
    int DiasAntecedenciaFatura,
    int DiasAtrasoSomenteLeitura,
    int AssinaturasAtivas,
    int AssinaturasEmTeste,
    decimal ReceitaMensalEstimada,
    decimal FaturadoNoMes,
    decimal RecebidoNoMes,
    decimal EmAberto,
    decimal Vencido,
    int InstituicoesEmAtraso,
    int InstituicoesSomenteLeitura,
    IReadOnlyList<FaturaResponse> FaturasVencidas
);

public record RotinaFaturamentoResponse(
    int FaturasGeradas,
    int CobrancasReenviadas,
    int FaturasVencidas,
    int InstituicoesAtualizadas,
    IReadOnlyList<string> Erros
);

// ---------- Área da instituição ----------

public record AvisoFinanceiroResponse(
    string SituacaoFinanceira,
    int DiasEmAtraso,
    DateOnly? VencimentoMaisAntigo,
    DateOnly? SomenteLeituraEm,
    bool PodeVerFaturas
);

public record MinhaFaturaResponse(
    int Id,
    DateOnly Competencia,
    DateOnly Vencimento,
    int AlunosAtivos,
    decimal Valor,
    string Situacao,
    string? LinkPagamento,
    DateOnly? PagaEm,
    string? NotaFiscalNumero,
    string? NotaFiscalPdfUrl
);

public record MinhaAssinaturaResponse(
    string PlanoNome,
    string Situacao,
    int DiaVencimento,
    DateOnly? TesteAte,
    string SituacaoFinanceira,
    IReadOnlyList<MinhaFaturaResponse> Faturas
);

// ---------- Resultado ----------

public enum FaturamentoErro
{
    None,
    Validation,
    NotFound,
    Conflict,
    Integracao
}

public record FaturamentoResult<T>(T? Value, string Message = "", FaturamentoErro Error = FaturamentoErro.None)
{
    public bool Success => Error == FaturamentoErro.None;
}
