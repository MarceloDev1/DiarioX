namespace DiarioX.Server.Domain.Entities;

public class Disciplina : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
    
    // Relacionamentos
    public ICollection<DisciplinaEtapaEnsino> EtapasEnsino { get; set; } = new List<DisciplinaEtapaEnsino>();

    /// <summary>Disciplina sem etapas vinculadas vale para qualquer etapa (mesma regra da alocação de professor).</summary>
    public bool FazParteDaGrade(Turma turma)
        => EtapasEnsino.Count == 0 || EtapasEnsino.Any(e => e.EtapaEnsinoId == turma.EtapaEnsinoId);
}
