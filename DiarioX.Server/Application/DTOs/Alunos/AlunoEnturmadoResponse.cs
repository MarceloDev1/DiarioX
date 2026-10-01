namespace DiarioX.Server.Application.DTOs.Alunos;

/// <summary>Aluno com enturmação ativa na turma.</summary>
public sealed record AlunoEnturmadoResponse(int AlunoId, string Matricula, string Nome, string Status, DateOnly DataInicio);
