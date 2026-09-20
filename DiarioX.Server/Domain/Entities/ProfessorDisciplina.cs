namespace DiarioX.Server.Domain.Entities;

public class ProfessorDisciplina
{
    public int Id { get; set; }
    public int ProfessorId { get; set; }
    public int DisciplinaId { get; set; }

    // Relacionamentos
    public Professor Professor { get; set; } = null!;
    public Disciplina Disciplina { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
