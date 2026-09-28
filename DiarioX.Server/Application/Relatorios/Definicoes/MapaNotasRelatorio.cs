using DiarioX.Server.Application.DTOs.Notas;
using DiarioX.Server.Application.Interfaces;
using static DiarioX.Server.Application.Relatorios.FormatacaoRelatorio;

namespace DiarioX.Server.Application.Relatorios.Definicoes;

/// <summary>Média atual de cada aluno em cada disciplina da turma, para o conselho de classe.</summary>
public class MapaNotasRelatorio : IRelatorio
{
    private readonly IRelatorioConsultas _consultas;
    private readonly INotaService _notaService;

    public MapaNotasRelatorio(IRelatorioConsultas consultas, INotaService notaService)
    {
        _consultas = consultas;
        _notaService = notaService;
    }

    public string Id => "mapa-notas";
    public string Nome => "Mapa de notas da turma";
    public string Descricao => "Média de cada aluno em cada disciplina (parcial até o último período lançado), com as disciplinas abaixo da média.";
    public string Categoria => "Pedagógico";
    public IReadOnlyList<string> Permissoes { get; } = [Auth.Permissoes.Notas.Visualizar];
    public bool Paisagem => true;

    public IReadOnlyList<FiltroDefinicao> Filtros { get; } =
    [
        new(FiltrosRelatorio.AnoLetivo, "Ano letivo"),
        new(FiltrosRelatorio.Escola, "Escola"),
        new(FiltrosRelatorio.Turma, "Turma", Obrigatorio: true),
    ];

    public async Task<RelatorioResultado<ConteudoRelatorio>> GerarAsync(RelatorioFiltros filtros, DateOnly hoje)
    {
        if (filtros.TurmaId is not int turmaId)
            return new(default, "Selecione a turma.", RelatorioErro.Validation);

        var turma = await _consultas.ObterTurmaAsync(turmaId);
        if (turma is null)
            return new(default, "Turma não encontrada.", RelatorioErro.NotFound);

        var resultado = await _notaService.GetMapaDaTurmaAsync(turmaId);
        if (!resultado.Success)
            return new(default, resultado.Message, RelatorioErro.NotFound);

        var mapa = resultado.Value!;
        var linhas = mapa.Alunos
            .Select((a, i) => new object?[] { i + 1, a.Matricula, a.Nome }
                .Concat(a.Medias.Cast<object?>())
                .Append(a.AbaixoDaMedia)
                .ToArray())
            .ToList();

        var colunas = new List<ColunaRelatorio> { new("Nº", TiposColuna.Inteiro), new("Matrícula"), new("Aluno") };
        colunas.AddRange(mapa.Disciplinas.Select(d => new ColunaRelatorio(d.Nome, TiposColuna.Decimal)));
        colunas.Add(new("Abaixo da média", TiposColuna.Inteiro));

        return new(new ConteudoRelatorio(
            [
                new("Turma", turma.Nome),
                new("Escola", turma.Escola),
                new("Ano letivo", turma.AnoReferencia.ToString()),
                new("Regra", DescreverRegra(mapa.Regra)),
                new("Posição em", hoje.ToString("dd/MM/yyyy", PtBr)),
            ],
            colunas,
            linhas,
            [
                new("Alunos", Inteiro(mapa.Alunos.Count)),
                new("Disciplinas", Inteiro(mapa.Disciplinas.Count)),
                new("Média para aprovação", mapa.Regra.MediaAprovacao.ToString("0.##", PtBr)),
                new("Alunos com disciplina abaixo da média", Inteiro(mapa.Alunos.Count(a => a.AbaixoDaMedia > 0))),
            ],
            MontarGrafico(mapa)));
    }

    private static string DescreverRegra(RegraAvaliacaoResumoResponse regra)
        => $"{regra.Nome} (0 a {regra.NotaMaxima.ToString("0.##", PtBr)}, média {regra.MediaAprovacao.ToString("0.##", PtBr)})";

    // Média da turma em cada disciplina (entre os alunos com nota).
    private static GraficoRelatorio? MontarGrafico(MapaNotasResponse mapa)
    {
        var medias = mapa.Disciplinas
            .Select((_, i) => mapa.Alunos.Select(a => a.Medias[i]).Where(m => m is not null).Select(m => m!.Value).ToList())
            .Select(valores => valores.Count == 0 ? 0m : Math.Round(valores.Average(), 1, MidpointRounding.AwayFromZero))
            .ToList();

        if (medias.All(m => m == 0))
            return null;

        return new GraficoRelatorio(
            "Média da turma por disciplina",
            mapa.Disciplinas.Select(d => d.Nome).ToList(),
            [new SerieGrafico("Média da turma", medias)]);
    }
}
