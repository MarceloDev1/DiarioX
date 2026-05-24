namespace DiarioX.Server.Domain.Entities;

public class Escola
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";

    public int Id { get; set; }
    public string CodigoInep { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;
    public int DiretorId { get; set; }
    public User Diretor { get; set; } = null!;
    public string Status { get; set; } = StatusAtivo;
}