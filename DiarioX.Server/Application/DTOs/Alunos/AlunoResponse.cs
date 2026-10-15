namespace DiarioX.Server.Application.DTOs.Alunos;

public record AlunoResponse(
    int Id,
    string Matricula,
    string Nome,
    DateTime DataNascimento,
    string Sexo,
    string? CorRaca,
    bool NecessidadeEspecial,
    string? CpfAluno,
    string? CertidaoNascimento,
    string ResponsavelNome1,
    string ResponsavelCpf1,
    string ResponsavelTelefone1,
    string? ResponsavelNome2,
    string Cep,
    string EnderecoCompleto,
    string Numero,
    string Bairro,
    int EscolaId,
    string EscolaNome,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    // Na lista de uma escola, o aluno que saiu dela por transferência para outra escola da rede: a linha
    // traz a escola consultada com status TRANSFERIDO, e aqui a escola em que ele está hoje.
    bool TransferidoDaEscola = false,
    string? EscolaAtualNome = null
);
