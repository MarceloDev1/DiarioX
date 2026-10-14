using System.Globalization;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Dashboard;
using DiarioX.Server.Application.DTOs.Dashboard;
using DiarioX.Server.Application.Interfaces;

namespace DiarioX.Server.Application.Services;

public class DashboardService : IDashboardService
{
    /// <summary>Janela da frequência média e do alerta de alunos infrequentes.</summary>
    public const int DiasFrequencia = 30;

    /// <summary>Janela da comparação de alunos ativos.</summary>
    public const int DiasVariacaoAlunos = 30;

    /// <summary>Turma/disciplina sem chamada há mais que isto gera alerta.</summary>
    public const int DiasSemChamada = 7;

    /// <summary>Aulas mínimas no período para o aluno entrar no alerta de infrequência.</summary>
    public const int MinimoAulasInfrequencia = 4;

    /// <summary>Antecedência do lembrete de fim de período avaliativo.</summary>
    public const int DiasAvisoFimPeriodo = 15;

    public const int DiasAgenda = 90;
    public const int MaximoEventosAgenda = 4;
    public const int QuantidadeUltimasChamadas = 5;

    /// <summary>RN02: frequência mínima para o alerta de infrequência do dashboard de gestão (80%).</summary>
    public const decimal LimiteInfrequenciaGestao = 80m;

    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly IDashboardConsultas _consultas;
    private readonly IPermissaoService _permissaoService;
    private readonly ITenantContext _tenantContext;

    public DashboardService(IDashboardConsultas consultas, IPermissaoService permissaoService, ITenantContext tenantContext)
    {
        _consultas = consultas;
        _permissaoService = permissaoService;
        _tenantContext = tenantContext;
    }

    public async Task<DashboardResponse> ObterAsync(UsuarioAtual usuario, DateOnly hoje, int? escolaId = null)
    {
        // RN01: listar escolas antes de restringir o escopo.
        var escolasDisponiveis = await _consultas.ListarEscolasAsync();
        var escolasOpcoes = escolasDisponiveis.Count > 1
            ? escolasDisponiveis.Select(e => new EscolaOpcao(e.Id, e.Nome)).ToList()
            : null;

        int? escolaSelecionada = null;
        if (escolaId is not null && escolasDisponiveis.Any(e => e.Id == escolaId))
        {
            escolaSelecionada = escolaId;
            _tenantContext.RestringirEscolas([escolaId.Value]);
        }

        var permissoes = await _permissaoService.GetPermissoesDoUsuarioAsync(usuario.UsuarioId, usuario.IsGlobalAdmin);
        var verAlunos = permissoes.Contains(Permissoes.Alunos.Visualizar);
        var verTurmas = permissoes.Contains(Permissoes.Turmas.Visualizar);
        var verChamada = permissoes.Contains(Permissoes.Chamada.Visualizar);
        var verProfessores = permissoes.Contains(Permissoes.Professores.Visualizar) ||
                             permissoes.Contains(Permissoes.AlocacaoProfessor.Visualizar);
        var verConteudo = permissoes.Contains(Permissoes.ConteudoMinistrado.Visualizar);

        var professorId = verChamada && !usuario.IsGlobalAdmin
            ? await _consultas.ObterProfessorIdDoUsuarioAsync(usuario.UsuarioId)
            : null;

        var alertas = new List<DashboardAlerta>();
        var frequenciaDe = hoje.AddDays(-(DiasFrequencia - 1));

        var indicadores = await _consultas.ObterIndicadoresAsync(new ParametrosIndicadores(
            hoje,
            hoje.AddDays(-DiasVariacaoAlunos),
            frequenciaDe,
            hoje.AddDays(-DiasSemChamada),
            ChamadaService.FrequenciaMinima,
            MinimoAulasInfrequencia,
            professorId));

        DashboardAlunos? alunos = null;
        if (verAlunos)
        {
            var ativos = indicadores.AlunosAtivos;
            var anteriores = indicadores.AlunosAtivosNaComparacao;
            decimal? variacao = anteriores == 0 ? null : Math.Round((ativos - anteriores) * 100m / anteriores, 1);
            alunos = new DashboardAlunos(ativos, variacao);
        }

        var turmas = verTurmas ? new DashboardTurmas(indicadores.TurmasAtivas, indicadores.EscolasComTurmas) : null;

        var professores = verProfessores
            ? new DashboardProfessores(indicadores.ProfessoresAlocados, indicadores.ProfessoresAtivos)
            : null;

        DashboardFrequencia? frequencia = null;
        IReadOnlyList<DashboardChamadaRecente>? ultimasChamadas = null;
        IReadOnlyList<FrequenciaDia>? graficoFrequencia = null;
        if (verChamada)
        {
            var aulas = indicadores.AulasRegistradas;
            decimal? percentual = aulas == 0 ? null : Math.Round(indicadores.Presencas * 100m / aulas, 1);
            frequencia = new DashboardFrequencia(percentual, ChamadaService.FrequenciaMinima, DiasFrequencia);

            if (indicadores.DisciplinasSemChamada > 0)
            {
                alertas.Add(new DashboardAlerta(TipoAlerta.Atencao,
                    $"Atenção: {Plural(indicadores.TurmasSemChamada, "turma", "turmas")} sem chamada há mais de {DiasSemChamada} dias " +
                    $"({Plural(indicadores.DisciplinasSemChamada, "disciplina", "disciplinas")}).",
                    "chamada"));
            }

            if (indicadores.AlunosInfrequentes > 0)
            {
                alertas.Add(new DashboardAlerta(TipoAlerta.Aviso,
                    $"{Plural(indicadores.AlunosInfrequentes, "aluno está", "alunos estão")} com frequência abaixo de " +
                    $"{ChamadaService.FrequenciaMinima:0}% nos últimos {DiasFrequencia} dias.",
                    "chamada"));
            }

            ultimasChamadas = (await _consultas.ListarUltimasChamadasAsync(QuantidadeUltimasChamadas, professorId))
                .Select(c => new DashboardChamadaRecente(c.Id, c.Turma, c.Disciplina, c.RegistradoPor, c.Data, c.Presentes, c.Alunos))
                .ToList();

            // RN03: gráfico de frequência diária (consolidado até ontem).
            var ontem = hoje.AddDays(-1);
            var graficoDe = ontem.AddDays(-(DiasFrequencia - 1));
            var pontos = await _consultas.ObterFrequenciaDiariaAsync(graficoDe, ontem);
            graficoFrequencia = pontos
                .Select(p => new FrequenciaDia(
                    p.Data.ToString("dd/MM"),
                    p.Aulas == 0 ? 0 : Math.Round(p.Presencas * 100m / p.Aulas, 1),
                    p.Aulas,
                    p.Presencas))
                .ToList();
        }

        // RN02: alunos em risco de infrequência (>20% de faltas = frequência < 80%).
        IReadOnlyList<AlunoEmRisco>? alunosEmRisco = null;
        if (verChamada && verAlunos)
        {
            var periodoAtual = (await _consultas.ListarMarcosDoCalendarioAsync(hoje.AddDays(-365), hoje.AddDays(365)))
                .Where(m => m.PeriodoNome is not null && m.Inicio <= hoje && m.Termino >= hoje)
                .OrderByDescending(m => m.Inicio)
                .FirstOrDefault();

            if (periodoAtual is not null)
            {
                var riscos = await _consultas.ListarAlunosEmRiscoAsync(
                    periodoAtual.Inicio, hoje, LimiteInfrequenciaGestao);
                alunosEmRisco = riscos
                    .Select(r => new AlunoEmRisco(r.AlunoId, r.Nome, r.Turma, r.Escola,
                        r.FrequenciaPercentual, r.TotalFaltas, r.TotalAulas, periodoAtual.PeriodoNome))
                    .ToList();
            }
        }

        // RN03: gráfico de diários (conteúdo ministrado × chamadas).
        DashboardDiarios? graficoDiarios = null;
        if (verChamada && verConteudo)
        {
            var ontem = hoje.AddDays(-1);
            var consolidado = await _consultas.ObterConsolidadoDiariosAsync(frequenciaDe, ontem);
            var pct = consolidado.TotalChamadas == 0
                ? 0m
                : Math.Round(consolidado.ChamadasComConteudo * 100m / consolidado.TotalChamadas, 1);
            graficoDiarios = new DashboardDiarios(
                consolidado.ChamadasComConteudo, consolidado.ChamadasSemConteudo, consolidado.TotalChamadas, pct);
        }

        if (verAlunos && indicadores.AlunosAguardandoEnturmacao > 0)
        {
            alertas.Add(new DashboardAlerta(TipoAlerta.Aviso,
                $"{Plural(indicadores.AlunosAguardandoEnturmacao, "aluno aguarda", "alunos aguardam")} enturmação.",
                "enturmar-aluno"));
        }

        var marcos = await _consultas.ListarMarcosDoCalendarioAsync(hoje, hoje.AddDays(DiasAgenda));

        foreach (var periodo in marcos
                     .Where(m => m.PeriodoNome is not null && m.Termino >= hoje && m.Termino <= hoje.AddDays(DiasAvisoFimPeriodo))
                     .OrderBy(m => m.Termino))
        {
            var dias = periodo.Termino.DayNumber - hoje.DayNumber;
            var quando = dias == 0 ? "termina hoje" : $"termina em {Plural(dias, "dia", "dias")} ({periodo.Termino:dd/MM})";
            alertas.Add(new DashboardAlerta(TipoAlerta.Info,
                $"{periodo.PeriodoNome} de {periodo.AnoReferencia} {quando}. Confira os lançamentos pendentes.",
                null));
        }

        return new DashboardResponse(
            alunos, turmas, frequencia, professores, alertas, ultimasChamadas,
            MontarAgenda(marcos, hoje),
            escolasOpcoes, escolaSelecionada, alunosEmRisco, graficoFrequencia, graficoDiarios);
    }

    private static IReadOnlyList<DashboardEvento> MontarAgenda(IEnumerable<MarcoCalendarioLinha> marcos, DateOnly hoje)
    {
        var ate = hoje.AddDays(DiasAgenda);
        bool NoIntervalo(DateOnly data) => data >= hoje && data <= ate;

        return marcos
            .SelectMany(m => m.PeriodoNome is null
                ? new[]
                {
                    new DashboardEvento(m.Inicio, TipoEvento.InicioAno, $"Início do ano letivo {m.AnoReferencia}", "Ano letivo"),
                    new DashboardEvento(m.Termino, TipoEvento.FimAno, $"Encerramento do ano letivo {m.AnoReferencia}", "Ano letivo"),
                }
                : new[]
                {
                    new DashboardEvento(m.Inicio, TipoEvento.InicioPeriodo, $"Início: {m.PeriodoNome}", $"Período avaliativo · {m.AnoReferencia}"),
                    new DashboardEvento(m.Termino, TipoEvento.FimPeriodo, $"Término: {m.PeriodoNome}", $"Período avaliativo · {m.AnoReferencia}"),
                })
            .Where(e => NoIntervalo(e.Data))
            .OrderBy(e => e.Data)
            .ThenBy(e => e.Titulo, StringComparer.Create(PtBr, ignoreCase: true))
            .Take(MaximoEventosAgenda)
            .ToList();
    }

    private static string Plural(int quantidade, string singular, string plural)
        => $"{quantidade} {(quantidade == 1 ? singular : plural)}";
}
