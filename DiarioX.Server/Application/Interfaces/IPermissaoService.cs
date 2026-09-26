using DiarioX.Server.Application.DTOs.Permissoes;

namespace DiarioX.Server.Application.Interfaces;

public interface IPermissaoService
{
    CatalogoPermissoesResponse GetCatalogo();

    /// <summary>Permissões de cada perfil atribuível na instituição atual.</summary>
    Task<IEnumerable<PerfilPermissoesResponse>> GetPerfisAsync();

    Task<PermissaoCommandResult> UpdatePerfilAsync(int perfilId, PerfilPermissoesRequest request);

    /// <summary>Permissões efetivas do usuário na instituição atual (todas, para o Administrador global).</summary>
    Task<IReadOnlySet<string>> GetPermissoesDoUsuarioAsync(int usuarioId, bool isGlobalAdmin);

    /// <summary>Grava a matriz padrão na instituição, se ela ainda não tiver nenhuma permissão.</summary>
    Task GarantirPadraoAsync(int tenantId);
}
