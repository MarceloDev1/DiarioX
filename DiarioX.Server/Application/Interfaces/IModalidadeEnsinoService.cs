using DiarioX.Server.Application.DTOs.ModalidadesEnsino;

namespace DiarioX.Server.Application.Interfaces;

public interface IModalidadeEnsinoService
{
    Task<IEnumerable<ModalidadeEnsinoResponse>> GetAllAsync();
    Task<ModalidadeEnsinoResponse?> GetByIdAsync(int id);
    Task<ModalidadeEnsinoCommandResult> CreateAsync(ModalidadeEnsinoRequest request);
    Task<ModalidadeEnsinoCommandResult> UpdateAsync(int id, ModalidadeEnsinoRequest request);
    Task<ModalidadeEnsinoCommandResult> DeleteAsync(int id);
}
