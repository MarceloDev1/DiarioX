namespace DiarioX.Server.Application.DTOs.Escolas;

public class EscolaRequest
{
    public string CodigoInep { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;
    public string Status { get; set; } = "ATIVO";
    public int? DiretorId { get; set; }
}
