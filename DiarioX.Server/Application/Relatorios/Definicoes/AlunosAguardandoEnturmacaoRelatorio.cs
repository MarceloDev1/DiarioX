using static DiarioX.Server.Application.Relatorios.FormatacaoRelatorio;

namespace DiarioX.Server.Application.Relatorios.Definicoes;

/// <summary>Fila de alunos cadastrados que ainda não foram enturmados, dos que esperam há mais tempo.</summary>
public class AlunosAguardandoEnturmacaoRelatorio : IRelatorio
{
    private readonly IRelatorioConsultas _consultas;

    public AlunosAguardandoEnturmacaoRelatorio(IRelatorioConsultas consultas)
    {
        _consultas = consultas;
    }

    public string Id => "alunos-aguardando-enturmacao";
    public string Nome => "Alunos aguardando enturmação";
    public string Descricao => "Alunos ativos sem turma, com há quantos dias estão esperando.";
    public string Categoria => "Secretaria";
    public IReadOnlyList<string> Permissoes { get; } = [Auth.Permissoes.Alunos.Visualizar];
    public bool Paisagem => false;

    public IReadOnlyList<FiltroDefinicao> Filtros { get; } = [new(FiltrosRelatorio.Escola, "Escola")];

    public async Task<RelatorioResultado<ConteudoRelatorio>> GerarAsync(RelatorioFiltros filtros, DateOnly hoje)
    {
        var aplicados = new List<FiltroAplicado>();
        if (filtros.EscolaId is int escolaId)
        {
            var escola = await _consultas.ObterNomeEscolaAsync(escolaId);
            if (escola is null)
                return new(default, "Escola não encontrada.", RelatorioErro.NotFound);
            aplicados.Add(new("Escola", escola));
        }

        var alunos = (await _consultas.ListarAlunosAguardandoEnturmacaoAsync(filtros.EscolaId))
            .Select(a => (Aluno: a, Dias: Math.Max(0, hoje.DayNumber - DateOnly.FromDateTime(a.CadastradoEm).DayNumber)))
            .OrderByDescending(x => x.Dias)
            .ThenBy(x => x.Aluno.Nome, StringComparer.Create(PtBr, ignoreCase: true))
            .ToList();

        var linhas = alunos
            .Select(x => new object?[]
            {
                x.Aluno.Matricula,
                x.Aluno.Nome,
                x.Aluno.Escola,
                Idade(x.Aluno.DataNascimento, hoje),
                DateOnly.FromDateTime(x.Aluno.CadastradoEm),
                x.Dias,
            })
            .ToList();

        var indicadores = new List<IndicadorRelatorio> { new("Alunos aguardando", Inteiro(alunos.Count)) };
        if (alunos.Count > 0)
        {
            indicadores.Add(new("Espera média (dias)", Inteiro((int)Math.Round(alunos.Average(x => x.Dias)))));
            indicadores.Add(new("Maior espera (dias)", Inteiro(alunos.Max(x => x.Dias))));
        }

        var porEscola = alunos.GroupBy(x => x.Aluno.Escola).OrderByDescending(g => g.Count()).ToList();
        var grafico = porEscola.Count > 1
            ? new GraficoRelatorio(
                "Alunos aguardando por escola",
                porEscola.Select(g => g.Key).ToList(),
                [new SerieGrafico("Alunos", porEscola.Select(g => (decimal)g.Count()).ToList())])
            : null;

        return new(new ConteudoRelatorio(
            aplicados,
            [
                new("Matrícula"),
                new("Aluno"),
                new("Escola"),
                new("Idade", TiposColuna.Inteiro),
                new("Cadastrado em", TiposColuna.Data),
                new("Dias aguardando", TiposColuna.Inteiro),
            ],
            linhas,
            indicadores,
            grafico));
    }
}
