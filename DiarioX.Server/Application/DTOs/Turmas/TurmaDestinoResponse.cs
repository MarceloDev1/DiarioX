namespace DiarioX.Server.Application.DTOs.Turmas;

/// <summary>Turma que pode receber alunos remanejados, com as vagas livres a partir da data da movimentação.</summary>
public sealed record TurmaDestinoResponse(int TurmaId, string NomeCompleto, string Turno, int VagasOfertadas, int VagasDisponiveis);
