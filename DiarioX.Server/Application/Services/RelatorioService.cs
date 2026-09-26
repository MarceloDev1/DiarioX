using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Relatorios;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class RelatorioService : IRelatorioService
{
    private readonly IReadOnlyList<IRelatorio> _relatorios;
    private readonly IReadOnlyList<IExportadorRelatorio> _exportadores;
    private readonly IRelatorioConsultas _consultas;
    private readonly IPermissaoService _permissaoService;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public RelatorioService(
        IEnumerable<IRelatorio> relatorios,
        IEnumerable<IExportadorRelatorio> exportadores,
        IRelatorioConsultas consultas,
        IPermissaoService permissaoService,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext)
    {
        _relatorios = relatorios.ToList();
        _exportadores = exportadores.ToList();
        _consultas = consultas;
        _permissaoService = permissaoService;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<RelatorioDefinicaoResponse>> ListarAsync(UsuarioAtual usuario)
    {
        var permissoes = await _permissaoService.GetPermissoesDoUsuarioAsync(usuario.UsuarioId, usuario.IsGlobalAdmin);

        return _relatorios
            .Where(r => r.Permissoes.All(permissoes.Contains))
            .OrderBy(r => r.Categoria, StringComparer.Create(FormatacaoRelatorio.PtBr, ignoreCase: true))
            .ThenBy(r => r.Nome, StringComparer.Create(FormatacaoRelatorio.PtBr, ignoreCase: true))
            .Select(r => new RelatorioDefinicaoResponse(r.Id, r.Nome, r.Descricao, r.Categoria, r.Filtros))
            .ToList();
    }

    public Task<OpcoesRelatorioResponse> ObterOpcoesAsync() => _consultas.ObterOpcoesAsync();

    public async Task<RelatorioResultado<RelatorioGerado>> GerarAsync(UsuarioAtual usuario, string id, RelatorioFiltros filtros)
    {
        var relatorio = _relatorios.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));
        if (relatorio is null)
            return new(default, "Relatório não encontrado.", RelatorioErro.NotFound);

        var permissoes = await _permissaoService.GetPermissoesDoUsuarioAsync(usuario.UsuarioId, usuario.IsGlobalAdmin);
        if (!relatorio.Permissoes.All(permissoes.Contains))
            return new(default, "Seu perfil não tem permissão para ver os dados deste relatório.", RelatorioErro.Forbidden);

        var agora = FormatacaoRelatorio.AgoraEmBrasilia();
        var conteudo = await relatorio.GerarAsync(filtros, DateOnly.FromDateTime(agora));
        if (!conteudo.Success)
            return new(default, conteudo.Message, conteudo.Error);

        var instituicao = _tenantContext.TenantId is int tenantId
            ? (await _tenantRepository.GetByIdAsync(tenantId))?.Nome
            : null;

        var dados = conteudo.Value!;
        return new(new RelatorioGerado(
            relatorio.Id,
            relatorio.Nome,
            instituicao ?? string.Empty,
            agora,
            relatorio.Paisagem,
            dados.Filtros,
            dados.Colunas,
            dados.Linhas,
            dados.Indicadores,
            dados.Grafico));
    }

    public RelatorioResultado<ArquivoRelatorio> Exportar(RelatorioGerado relatorio, string formato)
    {
        var exportador = _exportadores.FirstOrDefault(e => string.Equals(e.Formato, formato, StringComparison.OrdinalIgnoreCase));
        if (exportador is null)
        {
            var formatos = string.Join(", ", _exportadores.Select(e => e.Formato));
            return new(default, $"Formato inválido. Use: {formatos}.", RelatorioErro.Validation);
        }

        var nome = $"{relatorio.Id}-{relatorio.GeradoEm:yyyyMMdd-HHmm}.{exportador.Formato}";
        return new(new ArquivoRelatorio(exportador.Exportar(relatorio), exportador.ContentType, nome));
    }
}
