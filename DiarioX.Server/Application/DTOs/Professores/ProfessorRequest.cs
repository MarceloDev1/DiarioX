namespace DiarioX.Server.Application.DTOs.Professores;

public class ProfessorRequest
{
    // Dados Pessoais
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;

    // Dados Contratuais
    public string Matricula { get; set; } = string.Empty;
    public DateTime DataAdmissao { get; set; }
    public string Situacao { get; set; } = "ATIVO";

    // Relacionamentos
    public List<int> EscolaIds { get; set; } = new();
    public List<int> DisciplinaIds { get; set; } = new();
}
