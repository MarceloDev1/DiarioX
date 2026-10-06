namespace DiarioX.Server.Domain.Entities;

public class PeriodoAvaliativo : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int AnoLetivoId { get; set; }
    public AnoLetivo AnoLetivo { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public int Numero { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataTermino { get; set; }

    /// <summary>RF017 EX02: período consolidado e fechado pela coordenação; o diário não aceita mais alterações nele.</summary>
    public bool Encerrado { get; set; }
    public DateTime? EncerradoEm { get; set; }
}
