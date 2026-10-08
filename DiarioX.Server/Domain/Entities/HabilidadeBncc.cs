namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Habilidade da Base Nacional Comum Curricular cadastrada pela rede (RF018 RN02). Aponta para a disciplina
/// (componente curricular) e para as etapas de ensino (anos) em que vale, pois alguns códigos da BNCC
/// cobrem mais de um ano (ex.: EF67LP01).
/// </summary>
public class HabilidadeBncc : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    /// <summary>Código oficial da BNCC (ex.: EF06MA01).</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int DisciplinaId { get; set; }
    public Disciplina Disciplina { get; set; } = null!;

    /// <summary>Habilidade inativa deixa de ser sugerida, mas continua nos conteúdos já registrados.</summary>
    public bool Ativa { get; set; } = true;

    public ICollection<HabilidadeBnccEtapaEnsino> EtapasEnsino { get; set; } = new List<HabilidadeBnccEtapaEnsino>();
}

public class HabilidadeBnccEtapaEnsino : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int HabilidadeBnccId { get; set; }
    public HabilidadeBncc HabilidadeBncc { get; set; } = null!;
    public int EtapaEnsinoId { get; set; }
    public EtapaEnsino EtapaEnsino { get; set; } = null!;
}
