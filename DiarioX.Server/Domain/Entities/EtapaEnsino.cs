namespace DiarioX.Server.Domain.Entities;

public class EtapaEnsino
{
    public int Id { get; set; }
    public int ModalidadeEnsinoId { get; set; }
    public ModalidadeEnsino ModalidadeEnsino { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public int OrdemCronologica { get; set; }
    public int? IdadeRecomendada { get; set; }
}
