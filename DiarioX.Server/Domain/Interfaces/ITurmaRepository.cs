using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface ITurmaRepository
{
    Task<Turma?> GetByIdAsync(int id);
    Task<IEnumerable<Turma>> GetAllAsync();
    Task<bool> ExistsByCombinacaoAsync(
        int anoLetivoId,
        int escolaId,
        int etapaEnsinoId,
        string nomeIdentificador,
        string turno,
        int? excludeId = null);
    Task<Turma> AddAsync(Turma turma);
}