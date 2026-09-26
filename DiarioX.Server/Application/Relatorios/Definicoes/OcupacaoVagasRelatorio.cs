using DiarioX.Server.Domain.Entities;
using static DiarioX.Server.Application.Relatorios.FormatacaoRelatorio;

namespace DiarioX.Server.Application.Relatorios.Definicoes;

/// <summary>Vagas ofertadas × alunos enturmados em cada turma ativa, na data de hoje.</summary>
public class OcupacaoVagasRelatorio : IRelatorio
{
    private readonly IRelatorioConsultas _consultas;

    public OcupacaoVagasRelatorio(IRelatorioConsultas consultas)
    {
        _consultas = consultas;
    }

    public string Id => "ocupacao-vagas";
    public string Nome => "Ocupação de vagas";
    public string Descricao => "Vagas ofertadas, alunos enturmados e vagas disponíveis em cada turma ativa.";
    public string Categoria => "Secretaria";
    public IReadOnlyList<string> Permissoes { get; } = [Auth.Permissoes.Turmas.Visualizar];
    public bool Paisagem => false;

    public IReadOnlyList<FiltroDefinicao> Filtros { get; } =
    [
        new(FiltrosRelatorio.AnoLetivo, "Ano letivo"),
        new(FiltrosRelatorio.Escola, "Escola"),
        new(FiltrosRelatorio.Turno, "Turno"),
    ];

    public async Task<RelatorioResultado<ConteudoRelatorio>> GerarAsync(RelatorioFiltros filtros, DateOnly hoje)
    {
        var aplicados = new List<FiltroAplicado>();
        if (filtros.AnoLetivoId is int anoLetivoId)
        {
            var ano = await _consultas.ObterAnoReferenciaAsync(anoLetivoId);
            if (ano is null)
                return new(default, "Ano letivo não encontrado.", RelatorioErro.NotFound);
            aplicados.Add(new("Ano letivo", ano.Value.ToString()));
        }

        if (filtros.EscolaId is int escolaId)
        {
            var escola = await _consultas.ObterNomeEscolaAsync(escolaId);
            if (escola is null)
                return new(default, "Escola não encontrada.", RelatorioErro.NotFound);
            aplicados.Add(new("Escola", escola));
        }

        var turno = string.IsNullOrWhiteSpace(filtros.Turno) ? null : filtros.Turno.Trim().ToUpperInvariant();
        if (turno is not null)
        {
            if (turno is not (Turma.TurnoManha or Turma.TurnoTarde or Turma.TurnoNoite or Turma.TurnoIntegral))
                return new(default, "Turno inválido.", RelatorioErro.Validation);
            aplicados.Add(new("Turno", Turno(turno)));
        }

        aplicados.Add(new("Posição em", hoje.ToString("dd/MM/yyyy", PtBr)));

        var turmas = await _consultas.ListarOcupacaoDasTurmasAsync(filtros.AnoLetivoId, filtros.EscolaId, turno, hoje);

        var linhas = turmas
            .Select(t => new object?[]
            {
                t.Escola, t.Turma, Turno(t.Turno), t.Vagas, t.Enturmados, Disponiveis(t), Ocupacao(t.Enturmados, t.Vagas),
            })
            .ToList();

        var vagas = turmas.Sum(t => t.Vagas);
        var enturmados = turmas.Sum(t => t.Enturmados);
        var indicadores = new List<IndicadorRelatorio>
        {
            new("Turmas", Inteiro(turmas.Count)),
            new("Vagas ofertadas", Inteiro(vagas)),
            new("Alunos enturmados", Inteiro(enturmados)),
            new("Vagas disponíveis", Inteiro(turmas.Sum(Disponiveis))),
            new("Ocupação geral", Percentual(Ocupacao(enturmados, vagas))),
            new("Turmas lotadas", Inteiro(turmas.Count(t => t.Enturmados >= t.Vagas))),
        };

        return new(new ConteudoRelatorio(
            aplicados,
            [
                new("Escola"),
                new("Turma"),
                new("Turno"),
                new("Vagas", TiposColuna.Inteiro),
                new("Enturmados", TiposColuna.Inteiro),
                new("Disponíveis", TiposColuna.Inteiro),
                new("Ocupação", TiposColuna.Percentual),
            ],
            linhas,
            indicadores,
            MontarGrafico(turmas)));
    }

    // Uma barra por escola quando há mais de uma; senão, uma por turma.
    private static GraficoRelatorio? MontarGrafico(IReadOnlyList<OcupacaoTurmaLinha> turmas)
    {
        if (turmas.Count == 0)
            return null;

        var porEscola = turmas.Select(t => t.Escola).Distinct().Count() > 1;
        var grupos = turmas
            .GroupBy(t => porEscola ? t.Escola : t.Turma)
            .Select(g => (Nome: g.Key, Enturmados: g.Sum(t => t.Enturmados), Disponiveis: g.Sum(Disponiveis)))
            .ToList();

        return new GraficoRelatorio(
            porEscola ? "Vagas por escola" : "Vagas por turma",
            grupos.Select(g => g.Nome).ToList(),
            [
                new SerieGrafico("Enturmados", grupos.Select(g => (decimal)g.Enturmados).ToList()),
                new SerieGrafico("Disponíveis", grupos.Select(g => (decimal)g.Disponiveis).ToList()),
            ],
            Empilhado: true);
    }

    private static int Disponiveis(OcupacaoTurmaLinha turma) => Math.Max(0, turma.Vagas - turma.Enturmados);

    private static decimal Ocupacao(int enturmados, int vagas)
        => vagas == 0 ? 0m : Math.Round(enturmados * 100m / vagas, 1);
}
