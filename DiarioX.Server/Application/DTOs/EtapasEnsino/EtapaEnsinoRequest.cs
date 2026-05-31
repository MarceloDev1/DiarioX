namespace DiarioX.Server.Application.DTOs.EtapasEnsino;

public class EtapaEnsinoRequest
{
    public int ModalidadeEnsinoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public int OrdemCronologica { get; set; }
    public int? IdadeRecomendada { get; set; }
}
