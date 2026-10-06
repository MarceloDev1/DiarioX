namespace DiarioX.Server.Application.DTOs.EtapasEnsino;

public class EtapaEnsinoRequest
{
    public int ModalidadeEnsinoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public int OrdemCronologica { get; set; }
    public int? IdadeRecomendada { get; set; }

    /// <summary>POR_AULA (padrão) ou DIARIA: como a frequência das turmas da etapa é registrada (RF017 RN02).</summary>
    public string? TipoFrequencia { get; set; }
}
