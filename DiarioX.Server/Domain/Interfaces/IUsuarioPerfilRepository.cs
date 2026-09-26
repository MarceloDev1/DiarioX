using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IUsuarioPerfilRepository
{
    Task<IReadOnlyList<UsuarioPerfil>> GetByUsuarioIdAsync(int usuarioId);

    /// <summary>
    /// Define o perfil do usuário e as escolas em que ele atua: uma linha por escola ou, sem escolas,
    /// uma linha sem escola (acesso a todas as escolas da instituição).
    /// </summary>
    Task SubstituirAsync(int usuarioId, int perfilId, IReadOnlyCollection<int> escolaIds);
}
