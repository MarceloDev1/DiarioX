namespace DiarioX.Server.Domain.Entities;

public class ProfessorAlocacao
{
    public int Id { get; set; }
    public int ProfessorId { get; set; }
    public int TurmaId { get; set; }
    public int DisciplinaId { get; set; }
    public bool Ativa { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Professor Professor { get; set; } = null!;
    public Turma Turma { get; set; } = null!;
    public Disciplina Disciplina { get; set; } = null!;
}