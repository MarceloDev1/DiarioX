using DiarioX.Server.Application.Auth;

namespace DiarioX.Server.Application.DTOs.Permissoes;

public record CatalogoPermissoesResponse(
    IReadOnlyList<AcaoPermissao> Acoes,
    IReadOnlyList<ModuloPermissao> Modulos
);

public record PerfilPermissoesResponse(
    int PerfilId,
    string PerfilNome,
    string PerfilDescricao,
    IReadOnlyList<string> Permissoes
);

public class PerfilPermissoesRequest
{
    public List<string> Permissoes { get; set; } = new();
}

public enum PermissaoResultError
{
    None,
    Validation,
    NotFound
}

public record PermissaoCommandResult(
    bool Success,
    string Message,
    PerfilPermissoesResponse? Perfil = null,
    PermissaoResultError Error = PermissaoResultError.None
);
