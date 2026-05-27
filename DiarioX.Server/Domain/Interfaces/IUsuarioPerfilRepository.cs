using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IUsuarioPerfilRepository
{
    Task<UsuarioPerfil?> GetGlobalByUsuarioIdAsync(int usuarioId);
    Task<UsuarioPerfil> AddAsync(UsuarioPerfil usuarioPerfil);
    Task UpdateAsync(UsuarioPerfil usuarioPerfil);
}
