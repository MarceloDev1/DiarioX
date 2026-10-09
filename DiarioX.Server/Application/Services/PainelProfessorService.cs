using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Dashboard;
using DiarioX.Server.Application.DTOs.Dashboard;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using static DiarioX.Server.Application.Dashboard.AgendaProfessor;

namespace DiarioX.Server.Application.Services;

/// <summary>
/// Home do professor. Os dados vêm das alocações ativas do professor nas turmas do ano letivo vigente (o do
/// ano civil corrente), opcionalmente de uma escola só.
/// </summary>
public class PainelProfessorService : IPainelProfessorService
{
    public const string MensagemSemAulas = "Você não possui aulas agendadas para o dia de hoje.";
    public const string MensagemSemLotacao = "Nenhum diário de classe vinculado ao seu perfil para o Ano Letivo vigente.";

    private readonly IPainelProfessorConsultas _consultas;
    private readonly IPermissaoService _permissaoService;

    public PainelProfessorService(IPainelProfessorConsultas consultas, IPermissaoService permissaoService)
    {
        _consultas = consultas;
        _permissaoService = permissaoService;
    }

    public async Task<PainelProfessorResponse?> ObterAsync(UsuarioAtual usuario, DateOnly hoje, int? escolaId)
    {
        var contexto = await CarregarAsync(usuario, hoje, escolaId);
        if (contexto is null)
            return null;

        var (professor, ano, diarios, escolas, escolaFiltrada) = contexto;
        if (ano is null || diarios.Count == 0)
        {
            // EX02: sem lotação no ano letivo vigente.
            return new PainelProfessorResponse(
                professor.Nome, hoje.Year, ano?.DataInicio, ano?.DataTermino, MensagemSemLotacao, escolas, null,
                new AulasDoDiaResponse(hoje, false, null, MensagemSemLotacao, []), null, [], null, null, null);
        }

        var agenda = await MontarAgendaAsync(ano, diarios, ano.DataInicio, ano.DataTermino);
        var previstas = agenda.Previstas;

        var permissoes = await _permissaoService.GetPermissoesDoUsuarioAsync(usuario.UsuarioId, usuario.IsGlobalAdmin);
        var notas = permissoes.Contains(Permissoes.Notas.Visualizar)
            ? await MontarNotasAsync(ano, diarios, hoje)
            : null;

        return new PainelProfessorResponse(
            professor.Nome,
            ano.AnoReferencia,
            ano.DataInicio,
            ano.DataTermino,
            null,
            escolas,
            escolaFiltrada,
            MontarHoje(agenda, diarios, hoje),
            MontarMes(agenda, diarios, hoje, hoje.Year, hoje.Month),
            MontarDiarios(diarios, agenda.Grade),
            GraficoAulas(previstas, agenda.Registros, hoje),
            GraficoFrequencia(previstas, agenda.Registros, hoje),
            notas);
    }

    public async Task<PendenciasMesResponse?> ObterPendenciasAsync(UsuarioAtual usuario, DateOnly hoje, int? escolaId, int ano, int mes)
    {
        var contexto = await CarregarAsync(usuario, hoje, escolaId);
        if (contexto?.Ano is not { } anoLetivo || contexto.Diarios.Count == 0)
            return null;

        var inicio = new DateOnly(ano, mes, 1);
        var agenda = await MontarAgendaAsync(anoLetivo, contexto.Diarios, inicio, inicio.AddMonths(1).AddDays(-1));
        return MontarMes(agenda, contexto.Diarios, hoje, ano, mes);
    }

    private sealed record Contexto(
        ProfessorPainelLinha Professor,
        AnoLetivo? Ano,
        IReadOnlyList<DiarioLinha> Diarios,
        IReadOnlyList<EscolaPainelResponse> Escolas,
        int? EscolaId);

    private sealed record Agenda(
        IReadOnlyList<AulaPrevista> Previstas,
        Registros Registros,
        Calendarios Calendarios,
        ILookup<int, HorarioAula> Grade);

    private async Task<Contexto?> CarregarAsync(UsuarioAtual usuario, DateOnly hoje, int? escolaId)
    {
        if (usuario.IsGlobalAdmin)
            return null;

        var professor = await _consultas.ObterProfessorDoUsuarioAsync(usuario.UsuarioId);
        if (professor is null && !await _consultas.UsuarioTemPerfilProfessorAsync(usuario.UsuarioId))
            return null;

        // Perfil Professor sem cadastro de professor: home do professor sem lotação (EX02).
        professor ??= new ProfessorPainelLinha(0, string.Empty);

        var ano = await _consultas.ObterAnoLetivoAsync(hoje.Year);
        var diarios = ano is null || professor.Id == 0 ? [] : await _consultas.ListarDiariosAsync(professor.Id, ano.Id);

        var escolas = diarios
            .Select(d => new EscolaPainelResponse(d.EscolaId, d.EscolaNome))
            .DistinctBy(e => e.Id)
            .OrderBy(e => e.Nome)
            .ToList();

        // Escola que não é do professor é ignorada: vale "todas as escolas".
        var filtro = escolaId is int id && escolas.Any(e => e.Id == id) ? escolaId : null;
        var filtrados = filtro is null ? diarios : diarios.Where(d => d.EscolaId == filtro).ToList();

        return new Contexto(professor, ano, filtrados, escolas, filtro);
    }

    private async Task<Agenda> MontarAgendaAsync(AnoLetivo ano, IReadOnlyList<DiarioLinha> diarios, DateOnly de, DateOnly ate)
    {
        var turmaIds = diarios.Select(d => d.TurmaId).Distinct().ToList();
        var grade = (await _consultas.ListarGradeAsync(turmaIds)).ToLookup(h => h.TurmaId);
        var calendarios = new Calendarios(ano, await _consultas.ListarCalendariosPublicadosAsync(ano.Id));

        var turmas = diarios
            .GroupBy(d => d.TurmaId)
            .Select(g => new TurmaAgenda(
                g.Key, g.First().EscolaId, Diaria(g.First()), g.Select(d => d.DisciplinaId).ToHashSet(), grade[g.Key].ToList()))
            .ToList();

        var registros = new Registros(
            await _consultas.ListarChamadasAsync(turmaIds, de, ate),
            await _consultas.ListarConteudosAsync(turmaIds, de, ate));

        return new Agenda(Prever(turmas, de, ate, calendarios).ToList(), registros, calendarios, grade);
    }

    private static AulasDoDiaResponse MontarHoje(Agenda agenda, IReadOnlyList<DiarioLinha> diarios, DateOnly hoje)
    {
        var escolas = diarios.Select(d => d.EscolaId).Distinct().ToList();
        var letivo = escolas.Any(e => agenda.Calendarios.DiaLetivo(e, hoje));
        var evento = EventoDoDia(agenda.Calendarios, escolas, hoje);

        var aulas = agenda.Previstas
            .Where(a => a.Data == hoje)
            .Select(a => MapAula(a, diarios, agenda.Registros))
            .OrderBy(a => a.Tempos.Count == 0 ? int.MaxValue : a.Tempos[0])
            .ThenBy(a => a.TurmaNome)
            .ThenBy(a => a.DisciplinaNome)
            .ToList();

        // EX01: sem aula na grade (ou dia não letivo).
        return new AulasDoDiaResponse(hoje, letivo, evento, aulas.Count == 0 ? MensagemSemAulas : null, aulas);
    }

    /// <summary>RN03: cada dia letivo passado com frequência ou aula sem registro fica pendente.</summary>
    private static PendenciasMesResponse MontarMes(Agenda agenda, IReadOnlyList<DiarioLinha> diarios, DateOnly hoje, int ano, int mes)
    {
        var escolas = diarios.Select(d => d.EscolaId).Distinct().ToList();
        var porDia = agenda.Previstas.ToLookup(a => a.Data);
        var inicio = new DateOnly(ano, mes, 1);

        var dias = Enumerable.Range(0, DateTime.DaysInMonth(ano, mes))
            .Select(i => inicio.AddDays(i))
            .Select(data =>
            {
                var evento = EventoDoDia(agenda.Calendarios, escolas, data);
                if (!agenda.Calendarios.NoAnoLetivo(data))
                    return new DiaPendenciaResponse(data, SituacaoDia.ForaDoAno, evento, []);

                if (!escolas.Any(e => agenda.Calendarios.DiaLetivo(e, data)))
                    return new DiaPendenciaResponse(data, SituacaoDia.NaoLetivo, evento, []);

                var aulas = porDia[data].ToList();
                if (aulas.Count == 0)
                    return new DiaPendenciaResponse(data, data > hoje ? SituacaoDia.Futuro : SituacaoDia.SemAula, evento, []);

                if (data > hoje)
                    return new DiaPendenciaResponse(data, SituacaoDia.Futuro, evento, []);

                var pendencias = aulas
                    .Select(a => (Aula: a, SemFrequencia: !agenda.Registros.Frequencia(a), SemAula: !agenda.Registros.Aula(a)))
                    .Where(x => x.SemFrequencia || x.SemAula)
                    .Select(x =>
                    {
                        var (turma, disciplina) = Nomes(x.Aula, diarios);
                        return new PendenciaAulaResponse(x.Aula.TurmaId, turma, x.Aula.DisciplinaId, disciplina, x.SemFrequencia, x.SemAula);
                    })
                    .OrderBy(p => p.TurmaNome).ThenBy(p => p.DisciplinaNome)
                    .ToList();

                // Hoje ainda dá tempo de registrar: aparece destacado, mas não em vermelho.
                var situacao = data == hoje ? SituacaoDia.Hoje
                    : pendencias.Count > 0 ? SituacaoDia.Pendente
                    : SituacaoDia.EmDia;
                return new DiaPendenciaResponse(data, situacao, evento, pendencias);
            })
            .ToList();

        return new PendenciasMesResponse(ano, mes, dias);
    }

    private static IReadOnlyList<DiarioPainelResponse> MontarDiarios(IReadOnlyList<DiarioLinha> diarios, ILookup<int, HorarioAula> grade)
        => diarios
            .GroupBy(d => d.TurmaId)
            .Select(g =>
            {
                var turma = g.First();
                var diaria = Diaria(turma);
                var disciplinas = g
                    .OrderBy(d => d.DisciplinaNome)
                    .Select(d => new DiarioDisciplinaPainelResponse(
                        d.DisciplinaId, d.DisciplinaNome, grade[g.Key].Count(h => h.DisciplinaId == d.DisciplinaId)))
                    .ToList();

                return new DiarioPainelResponse(
                    turma.TurmaId, turma.TurmaNome, turma.EscolaNome, turma.EtapaNome, diaria,
                    !diaria && disciplinas.All(d => d.AulasSemanais == 0), disciplinas);
            })
            .OrderBy(d => d.EscolaNome).ThenBy(d => d.TurmaNome)
            .ToList();

    /// <summary>RN02: aulas registradas / aulas previstas no calendário do ano letivo.</summary>
    private static GraficoRegistrosResponse GraficoAulas(IReadOnlyList<AulaPrevista> previstas, Registros registros, DateOnly hoje)
    {
        var registradas = previstas.Count(registros.Aula);
        var pendentes = previstas.Count(a => a.Data < hoje && !registros.Aula(a));
        var aFazer = previstas.Count - registradas - pendentes;
        return new GraficoRegistrosResponse(registradas, pendentes, aFazer, Percentual(registradas, previstas.Count));
    }

    /// <summary>
    /// RN02: chamadas realizadas / dias letivos decorridos (aulas previstas até hoje). A chamada de hoje
    /// entra quando já foi feita; a que ainda falta não conta como atrasada.
    /// </summary>
    private static GraficoRegistrosResponse GraficoFrequencia(IReadOnlyList<AulaPrevista> previstas, Registros registros, DateOnly hoje)
    {
        var realizadas = previstas.Count(a => a.Data <= hoje && registros.Frequencia(a));
        var pendentes = previstas.Count(a => a.Data < hoje && !registros.Frequencia(a));
        return new GraficoRegistrosResponse(realizadas, pendentes, 0, Percentual(realizadas, realizadas + pendentes));
    }

    /// <summary>
    /// RN01: o período atual e os anteriores. Fica pendente a turma/disciplina sem avaliação no período ou
    /// com nota não lançada para aluno enturmado (avaliações já realizadas; a recuperação fica de fora, pois
    /// só vale para quem ficou abaixo da média). Alunos que saíram da turma são somente leitura nas notas.
    /// </summary>
    private async Task<IReadOnlyList<PeriodoNotasResponse>> MontarNotasAsync(
        AnoLetivo ano, IReadOnlyList<DiarioLinha> diarios, DateOnly hoje)
    {
        var periodos = ano.Periodos.Where(p => p.DataInicio <= hoje).OrderBy(p => p.Numero).ToList();
        if (periodos.Count == 0)
            return [];

        var turmaIds = diarios.Select(d => d.TurmaId).Distinct().ToList();
        var avaliacoes = (await _consultas.ListarAvaliacoesAsync(turmaIds, periodos.Select(p => p.Id).ToList()))
            .Where(a => !a.Recuperacao)
            .ToLookup(a => (a.TurmaId, a.DisciplinaId, a.PeriodoAvaliativoId));

        // Aluno que continua na turma: menor início entre as enturmações abertas.
        var alunos = (await _consultas.ListarEnturmacoesAsync(turmaIds, ano.DataInicio, ano.DataTermino))
            .Where(e => e.DataFim is null)
            .GroupBy(e => (e.TurmaId, e.AlunoId))
            .Select(g => (g.Key.TurmaId, g.Key.AlunoId, DataInicio: g.Min(e => e.DataInicio)))
            .ToLookup(e => e.TurmaId);

        return periodos
            .Select(periodo =>
            {
                var pendencias = diarios
                    .OrderBy(d => d.EscolaNome).ThenBy(d => d.TurmaNome).ThenBy(d => d.DisciplinaNome)
                    .Select(d =>
                    {
                        var doPeriodo = avaliacoes[(d.TurmaId, d.DisciplinaId, periodo.Id)].ToList();
                        if (doPeriodo.Count == 0)
                            return Pendencia(d, 0, 0, "Nenhuma avaliação cadastrada no período.");

                        var faltantesPorAvaliacao = doPeriodo
                            .Where(a => (a.Data ?? periodo.DataInicio) <= hoje)
                            .Select(a =>
                            {
                                var dataReferencia = a.Data ?? periodo.DataTermino;
                                var comNota = a.AlunosComNota.ToHashSet();
                                return alunos[d.TurmaId].Count(x => x.DataInicio <= dataReferencia && !comNota.Contains(x.AlunoId));
                            })
                            .Where(faltantes => faltantes > 0)
                            .ToList();

                        if (faltantesPorAvaliacao.Count == 0)
                            return null;

                        var total = faltantesPorAvaliacao.Sum();
                        return Pendencia(d, doPeriodo.Count, total,
                            $"{Plural(total, "nota não lançada", "notas não lançadas")} em " +
                            $"{Plural(faltantesPorAvaliacao.Count, "avaliação", "avaliações")}.");
                    })
                    .OfType<PendenciaNotaResponse>()
                    .ToList();

                return new PeriodoNotasResponse(
                    periodo.Id, periodo.Nome, periodo.Numero, periodo.DataInicio, periodo.DataTermino,
                    periodo.PrazoLancamentoNotas, periodo.PrazoLancamentoNotas is DateOnly prazo && prazo < hoje,
                    periodo.DataInicio <= hoje && hoje <= periodo.DataTermino, pendencias);
            })
            .ToList();

        static PendenciaNotaResponse Pendencia(DiarioLinha d, int avaliacoes, int faltantes, string descricao)
            => new(d.TurmaId, d.TurmaNome, d.EscolaNome, d.DisciplinaId, d.DisciplinaNome, avaliacoes, faltantes, descricao);
    }

    private static AulaDoDiaResponse MapAula(AulaPrevista aula, IReadOnlyList<DiarioLinha> diarios, Registros registros)
    {
        var (turma, disciplina) = Nomes(aula, diarios);
        var escola = diarios.First(d => d.TurmaId == aula.TurmaId).EscolaNome;
        return new AulaDoDiaResponse(
            aula.TurmaId, turma, escola, aula.DisciplinaId, disciplina, aula.Tempos,
            registros.Frequencia(aula), registros.Aula(aula));
    }

    private static (string Turma, string Disciplina) Nomes(AulaPrevista aula, IReadOnlyList<DiarioLinha> diarios)
    {
        var turma = diarios.First(d => d.TurmaId == aula.TurmaId).TurmaNome;
        var disciplina = aula.DisciplinaId is int id
            ? diarios.First(d => d.TurmaId == aula.TurmaId && d.DisciplinaId == id).DisciplinaNome
            : "Frequência diária";
        return (turma, disciplina);
    }

    /// <summary>Descrição do evento do calendário no dia (o da primeira escola que tiver um).</summary>
    private static string? EventoDoDia(Calendarios calendarios, IEnumerable<int> escolas, DateOnly data)
    {
        var evento = escolas.Select(e => calendarios.Evento(e, data)).FirstOrDefault(e => e is not null);
        if (evento is null)
            return null;

        var tipo = EventoCalendario.Tipos.GetValueOrDefault(evento.Tipo, evento.Tipo);
        return string.IsNullOrWhiteSpace(evento.Descricao) || evento.Descricao == tipo ? tipo : $"{tipo}: {evento.Descricao}";
    }

    private static bool Diaria(DiarioLinha diario) => diario.TipoFrequencia == EtapaEnsino.FrequenciaDiaria;

    private static decimal? Percentual(int parte, int total) => total == 0 ? null : Math.Round(parte * 100m / total, 1);

    private static string Plural(int quantidade, string singular, string plural)
        => $"{quantidade} {(quantidade == 1 ? singular : plural)}";
}
