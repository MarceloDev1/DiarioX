namespace DiarioX.Server.Application.DTOs.Disciplinas;

public class DisciplinaRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public List<int> EtapasEnsinoIds { get; set; } = new();
}
