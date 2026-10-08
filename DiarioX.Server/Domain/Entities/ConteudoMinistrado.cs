namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Conteúdo ministrado no diário (RF018): turma + disciplina + data, independente da chamada. O vínculo entre
/// os dois é conferido pelo diário (RN01), que sinaliza frequência sem conteúdo e conteúdo sem frequência.
/// </summary>
public class ConteudoMinistrado : ITenantEntity
{
    public const int MaxDescricao = 2000;

    public int Id { get; set; }
    public int TenantId { get; set; }
    public int TurmaId { get; set; }
    public Turma Turma { get; set; } = null!;
    public int DisciplinaId { get; set; }
    public Disciplina Disciplina { get; set; } = null!;
    public DateOnly Data { get; set; }
    public string Descricao { get; set; } = string.Empty;

    // Sem navegação: o Administrador global (usuário sem instituição) também registra, e o filtro de
    // tenant de User o esconderia num Include.
    public int RegistradoPorUsuarioId { get; set; }
    public int? AtualizadoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ConteudoMinistradoHabilidade> Habilidades { get; set; } = new List<ConteudoMinistradoHabilidade>();
}

public class ConteudoMinistradoHabilidade : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ConteudoMinistradoId { get; set; }
    public ConteudoMinistrado ConteudoMinistrado { get; set; } = null!;
    public int HabilidadeBnccId { get; set; }
    public HabilidadeBncc HabilidadeBncc { get; set; } = null!;
}
