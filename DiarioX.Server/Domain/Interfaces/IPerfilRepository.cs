using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IPerfilRepository
{
    Task<IEnumerable<Perfil>> GetAllAsync();
    Task<Perfil?> GetByIdAsync(int id);
}
