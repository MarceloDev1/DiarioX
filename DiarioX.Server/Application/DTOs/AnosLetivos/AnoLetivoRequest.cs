namespace DiarioX.Server.Application.DTOs.AnosLetivos;

public class PeriodoAvaliativoRequest
{
    public string Nome { get; set; } = string.Empty;
    public int Numero { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataTermino { get; set; }
}

public class AnoLetivoRequest
{
    public int AnoReferencia { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataTermino { get; set; }
    public string TipoPeriodo { get; set; } = string.Empty;
    public List<PeriodoAvaliativoRequest> Periodos { get; set; } = new();
}
