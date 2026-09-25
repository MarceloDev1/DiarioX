namespace DiarioX.Server.Domain.Entities;

public class AnoLetivo : ITenantEntity
{
    public const string TipoBimestral = "BIMESTRAL";
    public const string TipoTrimestral = "TRIMESTRAL";
    public const string TipoSemestral = "SEMESTRAL";

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int AnoReferencia { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataTermino { get; set; }
    public string TipoPeriodo { get; set; } = string.Empty;

    public ICollection<PeriodoAvaliativo> Periodos { get; set; } = new List<PeriodoAvaliativo>();
}
