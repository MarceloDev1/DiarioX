namespace DiarioX.Server.Domain.Entities;

public class ModalidadeEnsino
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";

    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string? CodigoMec { get; set; }
    public string? Descricao { get; set; }
    public string Status { get; set; } = StatusAtivo;
}
