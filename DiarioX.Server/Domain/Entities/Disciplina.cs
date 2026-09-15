namespace DiarioX.Server.Domain.Entities;

public class Disciplina
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
    
    // Relacionamentos
    public ICollection<DisciplinaEtapaEnsino> EtapasEnsino { get; set; } = new List<DisciplinaEtapaEnsino>();
}
