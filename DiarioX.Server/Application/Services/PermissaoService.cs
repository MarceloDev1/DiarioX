using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Permissoes;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class PermissaoService : IPermissaoService
{
    private readonly IPerfilPermissaoRepository _perfilPermissaoRepository;
    private readonly IPerfilRepository _perfilRepository;

    public PermissaoService(IPerfilPermissaoRepository perfilPermissaoRepository, IPerfilRepository perfilRepository)
    {
        _perfilPermissaoRepository = perfilPermissaoRepository;
        _perfilRepository = perfilRepository;
    }

    public CatalogoPermissoesResponse GetCatalogo() => new(Permissoes.Acoes, Permissoes.Modulos);

    public async Task<IEnumerable<PerfilPermissoesResponse>> GetPerfisAsync()
    {
        var perfis = await GetPerfisAtribuiveisAsync();
        var permissoes = await _perfilPermissaoRepository.GetAllAsync();
        var porPerfil = permissoes.ToLookup(pp => pp.PerfilId, pp => pp.Permissao);

        return perfis.Select(p => MapToResponse(p, porPerfil[p.Id]));
    }

    public async Task<PermissaoCommandResult> UpdatePerfilAsync(int perfilId, PerfilPermissoesRequest request)
    {
        var perfil = await _perfilRepository.GetByIdAsync(perfilId);
        if (perfil is null || IsAdministrador(perfil))
            return new PermissaoCommandResult(false, "Perfil não encontrado.", Error: PermissaoResultError.NotFound);

        var solicitadas = (request.Permissoes ?? new List<string>())
            .Select(p => (p ?? string.Empty).Trim())
            .ToHashSet(StringComparer.Ordinal);

        var invalidas = solicitadas.Where(p => !Permissoes.Todas.Contains(p)).ToList();
        if (invalidas.Count > 0)
            return Invalid($"Permissões inválidas: {string.Join(", ", invalidas)}.");

        // Quem cria, edita ou exclui precisa ao menos visualizar o módulo.
        foreach (var permissao in solicitadas.ToList())
        {
            var modulo = permissao[..permissao.LastIndexOf('.')];
            solicitadas.Add(Permissoes.Codigo(modulo, Permissoes.Visualizar));
        }

        // Evita que a instituição perca o acesso à própria tela de permissões.
        if (string.Equals(perfil.Nome, Perfil.Gerencia, StringComparison.OrdinalIgnoreCase) &&
            !solicitadas.Contains(Permissoes.Configuracoes.Editar))
        {
            return Invalid("O perfil Gerência precisa manter a permissão de editar Configurações.");
        }

        await _perfilPermissaoRepository.ReplaceAsync(perfilId, solicitadas);

        return new PermissaoCommandResult(
            true,
            $"Permissões do perfil {perfil.Nome} atualizadas com sucesso!",
            MapToResponse(perfil, solicitadas));
    }

    public async Task<IReadOnlySet<string>> GetPermissoesDoUsuarioAsync(int usuarioId, bool isGlobalAdmin)
    {
        if (isGlobalAdmin)
            return Permissoes.Todas;

        return await _perfilPermissaoRepository.GetPermissoesDoUsuarioAsync(usuarioId);
    }

    public async Task GarantirPadraoAsync(int tenantId)
    {
        if (await _perfilPermissaoRepository.TenantPossuiPermissoesAsync(tenantId))
            return;

        var perfis = await GetPerfisAtribuiveisAsync();
        var padrao = perfis
            .SelectMany(perfil => Permissoes.PadraoDoPerfil(perfil.Nome)
                .Select(permissao => new PerfilPermissao
                {
                    TenantId = tenantId,
                    PerfilId = perfil.Id,
                    Permissao = permissao,
                }))
            .ToList();

        if (padrao.Count > 0)
            await _perfilPermissaoRepository.AddRangeAsync(padrao);
    }

    // O Administrador é exclusivo dos usuários globais e não tem permissões configuráveis.
    private async Task<List<Perfil>> GetPerfisAtribuiveisAsync()
    {
        var perfis = await _perfilRepository.GetAllAsync();
        return perfis.Where(p => !IsAdministrador(p)).ToList();
    }

    private static bool IsAdministrador(Perfil perfil)
        => string.Equals(perfil.Nome, Perfil.Administrador, StringComparison.OrdinalIgnoreCase);

    private static PerfilPermissoesResponse MapToResponse(Perfil perfil, IEnumerable<string> permissoes)
        => new(perfil.Id, perfil.Nome, perfil.Descricao, permissoes.OrderBy(p => p, StringComparer.Ordinal).ToList());

    private static PermissaoCommandResult Invalid(string message)
        => new(false, message, Error: PermissaoResultError.Validation);
}
