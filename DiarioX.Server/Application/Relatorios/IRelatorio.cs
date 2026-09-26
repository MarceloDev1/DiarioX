namespace DiarioX.Server.Application.Relatorios;

/// <summary>
/// Um relatório do catálogo. Para criar um novo, implemente esta interface e registre-a no DI:
/// o catálogo, a tela, a exportação para Excel/PDF e a checagem de permissões vêm prontos.
/// </summary>
public interface IRelatorio
{
    /// <summary>Identificador usado na URL (ex.: "ocupacao-vagas").</summary>
    string Id { get; }
    string Nome { get; }
    string Descricao { get; }

    /// <summary>Agrupamento no catálogo (ex.: "Secretaria", "Pedagógico").</summary>
    string Categoria { get; }

    /// <summary>Permissões exigidas além de relatorios.visualizar (todas), ex.: alunos.visualizar.</summary>
    IReadOnlyList<string> Permissoes { get; }

    IReadOnlyList<FiltroDefinicao> Filtros { get; }

    /// <summary>Página em paisagem no PDF (tabelas largas).</summary>
    bool Paisagem { get; }

    Task<RelatorioResultado<ConteudoRelatorio>> GerarAsync(RelatorioFiltros filtros, DateOnly hoje);
}

/// <summary>Gera o arquivo de um relatório em um formato (ex.: "xlsx", "pdf").</summary>
public interface IExportadorRelatorio
{
    string Formato { get; }
    string ContentType { get; }
    byte[] Exportar(RelatorioGerado relatorio);
}
