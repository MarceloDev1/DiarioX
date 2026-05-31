namespace DiarioX.Server.Application.DTOs.ModalidadesEnsino;

public class ModalidadeEnsinoRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string? CodigoMec { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = "ATIVO";
}
