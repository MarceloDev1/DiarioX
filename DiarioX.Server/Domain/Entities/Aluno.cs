namespace DiarioX.Server.Domain.Entities;

public class Aluno : ITenantEntity
{
    public const string StatusAtivoAguardandoEnturmacao = "ATIVO_AGUARDANDO_ENTURMACAO";
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";

    public const string SexoMasculino = "MASCULINO";
    public const string SexoFeminino = "FEMININO";

    public int Id { get; set; }
    public int TenantId { get; set; }

    // RN02: gerada automaticamente (ano + sequencial) e imutável após a criação.
    public string Matricula { get; set; } = string.Empty;

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
    public Escola Escola { get; set; } = null!;

    public string Status { get; set; } = StatusAtivoAguardandoEnturmacao;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
