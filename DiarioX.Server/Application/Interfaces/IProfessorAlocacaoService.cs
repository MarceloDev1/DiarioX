using DiarioX.Server.Application.DTOs.Alocacoes;

namespace DiarioX.Server.Application.Interfaces;

public interface IProfessorAlocacaoService
{
    Task<IEnumerable<ProfessorAlocacaoResponse>> GetByProfessorAsync(int professorId);
    Task<IEnumerable<ProfessorAlocacaoDisponibilidadeResponse>> GetDisponiveisAsync(int professorId);
    Task<ProfessorAlocacaoCommandResult> CreateAsync(ProfessorAlocacaoRequest request);
    Task<ProfessorAlocacaoCommandResult> DeleteAsync(int id);
}

public enum ProfessorAlocacaoResultError
{
    Validation,
    NotFound,
    Conflict
}

public sealed record ProfessorAlocacaoCommandResult(
    bool Success,
    string Message,
    IEnumerable<ProfessorAlocacaoResponse>? Alocacoes = null,
    ProfessorAlocacaoResultError? Error = null);