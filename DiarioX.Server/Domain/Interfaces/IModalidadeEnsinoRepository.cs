using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IModalidadeEnsinoRepository
{
    Task<ModalidadeEnsino?> GetByIdAsync(int id);
    Task<IEnumerable<ModalidadeEnsino>> GetAllAsync();
    Task<bool> ExistsByNomeAsync(string nome, int? excludeId = null);
    Task<bool> ExistsBySiglaAsync(string sigla, int? excludeId = null);
    Task<ModalidadeEnsino> AddAsync(ModalidadeEnsino modalidade);
    Task UpdateAsync(ModalidadeEnsino modalidade);
    Task DeleteAsync(int id);
}
