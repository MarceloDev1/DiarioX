namespace DiarioX.Server.Domain.Entities;

public class ProfessorEscola : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ProfessorId { get; set; }
    public int EscolaId { get; set; }

    // Relacionamentos
    public Professor Professor { get; set; } = null!;
    public Escola Escola { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
