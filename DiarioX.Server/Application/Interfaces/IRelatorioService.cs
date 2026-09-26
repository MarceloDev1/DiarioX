using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Relatorios;

namespace DiarioX.Server.Application.Interfaces;

public interface IRelatorioService
{
    /// <summary>Relatórios que o usuário pode gerar (tem todas as permissões exigidas por eles).</summary>
    Task<IEnumerable<RelatorioDefinicaoResponse>> ListarAsync(UsuarioAtual usuario);

    Task<OpcoesRelatorioResponse> ObterOpcoesAsync();

    Task<RelatorioResultado<RelatorioGerado>> GerarAsync(UsuarioAtual usuario, string id, RelatorioFiltros filtros);

    /// <summary>Gera o arquivo do relatório no formato pedido ("xlsx" ou "pdf").</summary>
    RelatorioResultado<ArquivoRelatorio> Exportar(RelatorioGerado relatorio, string formato);
}
