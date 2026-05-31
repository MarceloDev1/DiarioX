namespace DiarioX.Server.Domain.Entities;

public class PeriodoAvaliativo
{
    public int Id { get; set; }
    public int AnoLetivoId { get; set; }
    public AnoLetivo AnoLetivo { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public int Numero { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataTermino { get; set; }
}
