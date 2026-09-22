namespace DiarioX.Server.Domain.Entities;

public class AlunoTurma
{
    public int Id { get; set; }
    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}