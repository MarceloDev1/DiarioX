namespace DiarioX.Server.Application.DTOs.Alocacoes;

public sealed record ProfessorAlocacaoItemRequest(int TurmaId, int DisciplinaId, int? SubstituirAlocacaoId = null);

public sealed record ProfessorAlocacaoRequest(int ProfessorId, List<ProfessorAlocacaoItemRequest> Itens);