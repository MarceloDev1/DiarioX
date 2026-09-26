namespace DiarioX.Server.Application.Relatorios;

/// <summary>Tipos de coluna: definem a formatação na tela e o tipo da célula no Excel e no PDF.</summary>
public static class TiposColuna
{
    public const string Texto = "texto";
    public const string Inteiro = "inteiro";
    public const string Decimal = "decimal";

    /// <summary>Valor de 0 a 100 (ex.: 87.5 = 87,5%).</summary>
    public const string Percentual = "percentual";

    /// <summary>DateOnly.</summary>
    public const string Data = "data";
}

/// <summary>Filtros que os relatórios aceitam. O frontend sabe montar cada um a partir de /api/relatorios/opcoes.</summary>
public static class FiltrosRelatorio
{
    public const string AnoLetivo = "anoLetivoId";
    public const string Escola = "escolaId";
    public const string Turma = "turmaId";
    public const string Turno = "turno";
}

public record ColunaRelatorio(string Titulo, string Tipo = TiposColuna.Texto);

public record IndicadorRelatorio(string Rotulo, string Valor);

public record SerieGrafico(string Nome, IReadOnlyList<decimal> Valores);

/// <summary>Gráfico de barras opcional exibido na tela junto com a tabela.</summary>
public record GraficoRelatorio(
    string Titulo,
    IReadOnlyList<string> Categorias,
    IReadOnlyList<SerieGrafico> Series,
    bool Empilhado = false
);

/// <summary>Filtro aplicado, já com o texto que aparece no cabeçalho (ex.: "Escola: EM Monteiro Lobato").</summary>
public record FiltroAplicado(string Rotulo, string Valor);

public record FiltroDefinicao(string Chave, string Rotulo, bool Obrigatorio = false);

/// <summary>Dados de um relatório. Cada linha traz um valor por coluna, na mesma ordem.</summary>
public record ConteudoRelatorio(
    IReadOnlyList<FiltroAplicado> Filtros,
    IReadOnlyList<ColunaRelatorio> Colunas,
    IReadOnlyList<object?[]> Linhas,
    IReadOnlyList<IndicadorRelatorio> Indicadores,
    GraficoRelatorio? Grafico = null
);

/// <summary>Relatório pronto para exibir ou exportar.</summary>
public record RelatorioGerado(
    string Id,
    string Titulo,
    string Instituicao,
    DateTime GeradoEm,
    bool Paisagem,
    IReadOnlyList<FiltroAplicado> Filtros,
    IReadOnlyList<ColunaRelatorio> Colunas,
    IReadOnlyList<object?[]> Linhas,
    IReadOnlyList<IndicadorRelatorio> Indicadores,
    GraficoRelatorio? Grafico
);

public class RelatorioFiltros
{
    public int? AnoLetivoId { get; set; }
    public int? EscolaId { get; set; }
    public int? TurmaId { get; set; }
    public string? Turno { get; set; }
}

public record RelatorioDefinicaoResponse(
    string Id,
    string Nome,
    string Descricao,
    string Categoria,
    IReadOnlyList<FiltroDefinicao> Filtros
);

public record OpcaoEscola(int Id, string Nome);

public record OpcaoAnoLetivo(int Id, int AnoReferencia);

public record OpcaoTurma(int Id, string Nome, int EscolaId, int AnoLetivoId, string Turno, bool Ativa);

/// <summary>Opções dos filtros, já restritas às escolas do usuário.</summary>
public record OpcoesRelatorioResponse(
    IReadOnlyList<OpcaoEscola> Escolas,
    IReadOnlyList<OpcaoAnoLetivo> AnosLetivos,
    IReadOnlyList<OpcaoTurma> Turmas
);

public record ArquivoRelatorio(byte[] Conteudo, string ContentType, string NomeArquivo);

public enum RelatorioErro
{
    None,
    Validation,
    NotFound,
    Forbidden
}

public record RelatorioResultado<T>(T? Value, string Message = "", RelatorioErro Error = RelatorioErro.None)
{
    public bool Success => Error == RelatorioErro.None;
}
