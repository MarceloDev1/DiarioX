namespace DiarioX.Server.Application.DTOs.RegrasAvaliacao;

public class RegraAvaliacaoRequest
{
    public string Nome { get; set; } = string.Empty;
    public decimal NotaMaxima { get; set; }
    public decimal MediaAprovacao { get; set; }
    public int CasasDecimais { get; set; }
    public string CalculoNotaPeriodo { get; set; } = string.Empty;
    public bool PermiteRecuperacao { get; set; }

    /// <summary>SUBSTITUI_MEDIA (padrão), MEDIA_COM_RECUPERACAO ou LIMITADA_A_MEDIA_APROVACAO.</summary>
    public string? SubstituicaoRecuperacao { get; set; }
    public List<int> EtapaEnsinoIds { get; set; } = new();
}

public record RegraEtapaResponse(int Id, string Nome, string ModalidadeNome);

/// <summary>Id nulo = regra padrão do sistema, usada pelas etapas sem regra própria.</summary>
public record RegraAvaliacaoResponse(
    int? Id,
    string Nome,
    decimal NotaMaxima,
    decimal MediaAprovacao,
    int CasasDecimais,
    string CalculoNotaPeriodo,
    bool PermiteRecuperacao,
    string SubstituicaoRecuperacao,
    IReadOnlyList<RegraEtapaResponse> Etapas
);

public enum RegraAvaliacaoResultError
{
    None,
    Validation,
    NotFound,
    Conflict
}

public record RegraAvaliacaoCommandResult(
    bool Success,
    string Message,
    RegraAvaliacaoResponse? Regra = null,
    RegraAvaliacaoResultError Error = RegraAvaliacaoResultError.None
);
