namespace DiarioX.Server.Application.DTOs.Alunos;

public class AlunoRequest
{
    // Dados do Aluno
    public string Nome { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string? CorRaca { get; set; }
    public bool NecessidadeEspecial { get; set; }

    // Dados dos Responsáveis
    public string ResponsavelNome1 { get; set; } = string.Empty;
    public string ResponsavelCpf1 { get; set; } = string.Empty;
    public string ResponsavelTelefone1 { get; set; } = string.Empty;
    public string? ResponsavelNome2 { get; set; }

    // Documentação/Endereço
    public string? CpfAluno { get; set; }
    public string? CertidaoNascimento { get; set; }
    public string Cep { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;

    public int EscolaId { get; set; }
}
