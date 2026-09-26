using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface IPerfilPermissaoRepository
{
    /// <summary>Permissões de todos os perfis na instituição atual.</summary>
    Task<IReadOnlyList<PerfilPermissao>> GetAllAsync();

    /// <summary>União das permissões dos perfis do usuário na instituição atual.</summary>
    Task<IReadOnlySet<string>> GetPermissoesDoUsuarioAsync(int usuarioId);

    /// <summary>Substitui as permissões do perfil na instituição atual.</summary>
    Task ReplaceAsync(int perfilId, IEnumerable<string> permissoes);

    /// <summary>Indica se a instituição já tem alguma permissão gravada (ignora o tenant da requisição).</summary>
    Task<bool> TenantPossuiPermissoesAsync(int tenantId);

    /// <summary>Grava permissões de uma instituição informada explicitamente (seed).</summary>
    Task AddRangeAsync(IEnumerable<PerfilPermissao> permissoes);
}
