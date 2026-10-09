namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Um tempo de aula da grade semanal da turma: dia da semana + ordem do tempo (1º, 2º...) → disciplina.
/// Uma aula dupla ocupa dois tempos seguidos da mesma disciplina. A grade define as aulas previstas no
/// calendário (dias letivos × tempos) usadas pelo painel do professor.
/// </summary>
public class HorarioAula : ITenantEntity
{
    /// <summary>Segunda a sábado (valores de DayOfWeek); o sábado só tem aula em Dia Letivo Especial.</summary>
    public const int PrimeiroDia = (int)DayOfWeek.Monday;
    public const int UltimoDia = (int)DayOfWeek.Saturday;
    public const int MaxTempos = 10;

    public int Id { get; set; }
    public int TenantId { get; set; }

    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;

    /// <summary>Dia da semana (1 = segunda ... 6 = sábado).</summary>
    public int DiaSemana { get; set; }

    /// <summary>Ordem do tempo de aula no dia (1 a MaxTempos).</summary>
    public int Ordem { get; set; }

    public int DisciplinaId { get; set; }
    public Disciplina Disciplina { get; set; } = null!;
}
