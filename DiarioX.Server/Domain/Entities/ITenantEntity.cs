namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Entidade que pertence a uma instituição (tenant). O AppDbContext filtra as consultas
/// pelo tenant da requisição e preenche o TenantId automaticamente nas inclusões.
/// </summary>
public interface ITenantEntity
{
    int TenantId { get; set; }
}
