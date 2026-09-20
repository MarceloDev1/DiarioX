namespace DiarioX.Server.Application.DTOs.Professores;

public record DisciplinaResponseForProfessor(
    int Id,
    string Nome
);

public record ProfessorResponse(
    int Id,
    string Nome,
    string Cpf,
    DateTime DataNascimento,
    string Email,
    string Telefone,
    string Matricula,
    DateTime DataAdmissao,
    string Situacao,
    int EscolaId,
    string EscolaNome,
    List<DisciplinaResponseForProfessor> Disciplinas,
    int? UsuarioId,
    string? UsuarioEmail,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
