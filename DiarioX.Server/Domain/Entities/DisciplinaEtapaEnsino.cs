namespace DiarioX.Server.Domain.Entities;

public class DisciplinaEtapaEnsino : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int DisciplinaId { get; set; }
    public Disciplina Disciplina { get; set; } = null!;
    public int EtapaEnsinoId { get; set; }
    public EtapaEnsino EtapaEnsino { get; set; } = null!;
}
