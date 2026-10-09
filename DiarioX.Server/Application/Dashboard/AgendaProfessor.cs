using DiarioX.Server.Application.Calendario;
using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.Dashboard;

/// <summary>
/// Aulas previstas do professor: a grade de horários das turmas cruzada com o Calendário Letivo (RN02).
/// A unidade é a aula do dia (turma + disciplina + data, com os tempos que ela ocupa), a mesma do registro
/// de frequência e do conteúdo ministrado. Na frequência diária (Anos Iniciais) a unidade é a turma no dia
/// letivo, com ou sem grade.
/// </summary>
public static class AgendaProfessor
{
    /// <param name="Disciplinas">Disciplinas em que o professor está alocado na turma.</param>
    public sealed record TurmaAgenda(int TurmaId, int EscolaId, bool Diaria, IReadOnlySet<int> Disciplinas, IReadOnlyList<HorarioAula> Grade);

    /// <param name="DisciplinaId">Nulo na frequência diária.</param>
    public sealed record AulaPrevista(int TurmaId, int? DisciplinaId, DateOnly Data, IReadOnlyList<int> Tempos);

    public static IEnumerable<AulaPrevista> Prever(
        IEnumerable<TurmaAgenda> turmas, DateOnly de, DateOnly ate, Calendarios calendarios)
    {
        foreach (var turma in turmas)
        {
            var gradePorDia = turma.Grade
                .Where(h => turma.Disciplinas.Contains(h.DisciplinaId))
                .ToLookup(h => h.DiaSemana);

            for (var data = de; data <= ate; data = data.AddDays(1))
            {
                if (!calendarios.DiaLetivo(turma.EscolaId, data))
                    continue;

                if (turma.Diaria)
                {
                    yield return new AulaPrevista(turma.TurmaId, null, data, []);
                    continue;
                }

                foreach (var disciplina in gradePorDia[(int)data.DayOfWeek].GroupBy(h => h.DisciplinaId))
                    yield return new AulaPrevista(turma.TurmaId, disciplina.Key, data, disciplina.Select(h => h.Ordem).Order().ToList());
            }
        }
    }

    /// <summary>
    /// Calendário efetivo de cada escola no ano letivo: os eventos publicados da rede com os da escola por
    /// cima. Sem calendário publicado vale a regra padrão (segunda a sexta letivos). Fora do ano letivo não
    /// há dia letivo.
    /// </summary>
    public sealed class Calendarios
    {
        private readonly AnoLetivo _ano;
        private readonly CalendarioLetivo? _rede;
        private readonly IReadOnlyList<CalendarioLetivo> _publicados;
        private readonly Dictionary<int, Dictionary<DateOnly, EventoCalendario>> _porEscola = new();

        public Calendarios(AnoLetivo ano, IReadOnlyList<CalendarioLetivo> publicados)
        {
            _ano = ano;
            _publicados = publicados;
            _rede = publicados.FirstOrDefault(c => c.EscolaId is null);
        }

        public bool NoAnoLetivo(DateOnly data) => data >= _ano.DataInicio && data <= _ano.DataTermino;

        public bool DiaLetivo(int escolaId, DateOnly data)
            => NoAnoLetivo(data) && CalendarioEfetivo.DiaLetivo(data, Evento(escolaId, data));

        public EventoCalendario? Evento(int escolaId, DateOnly data) => Eventos(escolaId).GetValueOrDefault(data);

        private Dictionary<DateOnly, EventoCalendario> Eventos(int escolaId)
        {
            if (!_porEscola.TryGetValue(escolaId, out var eventos))
            {
                eventos = CalendarioEfetivo.Mesclar(_rede, _publicados.FirstOrDefault(c => c.EscolaId == escolaId));
                _porEscola[escolaId] = eventos;
            }
            return eventos;
        }
    }

    /// <summary>Registros do diário: chamadas (frequência) e conteúdos ministrados (aula).</summary>
    public sealed class Registros
    {
        private readonly HashSet<(int TurmaId, int? DisciplinaId, DateOnly Data)> _chamadas;
        private readonly HashSet<(int TurmaId, int? DisciplinaId, DateOnly Data)> _conteudos;
        private readonly HashSet<(int TurmaId, DateOnly Data)> _conteudosDaTurma;

        public Registros(IEnumerable<RegistroDiarioLinha> chamadas, IEnumerable<RegistroDiarioLinha> conteudos)
        {
            _chamadas = chamadas.Select(c => (c.TurmaId, c.DisciplinaId, c.Data)).ToHashSet();
            var lista = conteudos.ToList();
            _conteudos = lista.Select(c => (c.TurmaId, c.DisciplinaId, c.Data)).ToHashSet();
            _conteudosDaTurma = lista.Select(c => (c.TurmaId, c.Data)).ToHashSet();
        }

        public bool Frequencia(AulaPrevista aula) => _chamadas.Contains((aula.TurmaId, aula.DisciplinaId, aula.Data));

        /// <summary>
        /// Na frequência diária a aula do dia está registrada com o conteúdo de qualquer disciplina da turma,
        /// como no diário do conteúdo ministrado (RN01).
        /// </summary>
        public bool Aula(AulaPrevista aula) => aula.DisciplinaId is null
            ? _conteudosDaTurma.Contains((aula.TurmaId, aula.Data))
            : _conteudos.Contains((aula.TurmaId, aula.DisciplinaId, aula.Data));
    }
}
