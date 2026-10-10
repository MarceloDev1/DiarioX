using DiarioX.Server.Application.DTOs.Horarios;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

/// <summary>
/// Grade semanal de cada turma: em cada dia (segunda a sábado) e tempo de aula, a disciplina ministrada.
/// O painel do professor cruza a grade com o Calendário Letivo para saber as aulas previstas.
/// </summary>
public class HorarioAulaService : IHorarioAulaService
{
    private readonly IHorarioAulaRepository _horarioRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IProfessorAlocacaoRepository _alocacaoRepository;

    public HorarioAulaService(
        IHorarioAulaRepository horarioRepository,
        ITurmaRepository turmaRepository,
        IDisciplinaRepository disciplinaRepository,
        IProfessorAlocacaoRepository alocacaoRepository)
    {
        _horarioRepository = horarioRepository;
        _turmaRepository = turmaRepository;
        _disciplinaRepository = disciplinaRepository;
        _alocacaoRepository = alocacaoRepository;
    }

    public async Task<IReadOnlyList<HorarioTurmaResumoResponse>> GetTurmasAsync()
    {
        var turmas = (await _turmaRepository.GetAllAsync()).Where(t => t.Status == Turma.StatusAtivo).ToList();
        var tempos = (await _horarioRepository.ListByTurmasAsync(turmas.Select(t => t.Id).ToList()))
            .GroupBy(h => h.TurmaId)
            .ToDictionary(g => g.Key, g => g.Count());

        return turmas
            .OrderByDescending(t => t.AnoLetivo.AnoReferencia)
            .ThenBy(t => t.Escola.Nome)
            .ThenBy(t => t.NomeCompleto)
            .Select(t => new HorarioTurmaResumoResponse(
                t.Id, t.NomeCompleto, t.Escola.Nome, t.AnoLetivo.AnoReferencia, t.EtapaEnsino.TipoFrequencia,
                tempos.GetValueOrDefault(t.Id)))
            .ToList();
    }

    public async Task<HorarioResult<HorarioTurmaResponse>> GetAsync(int turmaId)
    {
        var turma = await _turmaRepository.GetByIdAsync(turmaId);
        if (turma is null)
            return NaoEncontrada();

        return new(await MontarAsync(turma));
    }

    public async Task<HorarioResult<HorarioTurmaResponse>> SalvarAsync(int turmaId, HorarioRequest request)
    {
        var turma = await _turmaRepository.GetByIdAsync(turmaId);
        if (turma is null)
            return NaoEncontrada();

        var tempos = request.Tempos ?? [];
        if (tempos.Any(t => t.DiaSemana is < HorarioAula.PrimeiroDia or > HorarioAula.UltimoDia))
            return Invalida("Dia da semana inválido: use de segunda a sábado.");

        if (tempos.Any(t => t.Ordem is < 1 or > HorarioAula.MaxTempos))
            return Invalida($"O tempo de aula deve estar entre 1 e {HorarioAula.MaxTempos}.");

        if (tempos.GroupBy(t => (t.DiaSemana, t.Ordem)).Any(g => g.Count() > 1))
            return Invalida("Cada tempo de aula comporta uma única disciplina.");

        var permitidas = (await DisciplinasDaTurmaAsync(turma)).Select(d => d.Id).ToHashSet();
        if (tempos.Any(t => !permitidas.Contains(t.DisciplinaId)))
            return Invalida("A grade só aceita disciplinas ativas da etapa de ensino da turma.");

        await _horarioRepository.SubstituirAsync(turmaId, tempos
            .Select(t => new HorarioAula { DiaSemana = t.DiaSemana, Ordem = t.Ordem, DisciplinaId = t.DisciplinaId })
            .ToList());

        return new(await MontarAsync(turma));
    }

    private async Task<HorarioTurmaResponse> MontarAsync(Turma turma)
    {
        var tempos = await _horarioRepository.ListByTurmaAsync(turma.Id);
        var professores = (await _alocacaoRepository.GetAtivasDaTurmaAsync(turma.Id))
            .ToLookup(a => a.DisciplinaId, a => a.Professor.Nome);

        var disciplinas = (await DisciplinasDaTurmaAsync(turma))
            .Select(d => (d.Id, d.Nome))
            // Disciplina que saiu da etapa (ou foi inativada) continua aparecendo onde já está na grade.
            .Concat(tempos.Select(t => (t.DisciplinaId, t.Disciplina.Nome)))
            .DistinctBy(d => d.Item1)
            .OrderBy(d => d.Item2)
            .Select(d => new HorarioDisciplinaResponse(d.Item1, d.Item2, professores[d.Item1].Order().ToList()))
            .ToList();

        return new HorarioTurmaResponse(
            turma.Id, turma.NomeCompleto, turma.Escola.Nome, turma.AnoLetivo.AnoReferencia, turma.EtapaEnsino.TipoFrequencia,
            HorarioAula.MaxTempos, disciplinas,
            tempos.Select(t => new HorarioTempoDto(t.DiaSemana, t.Ordem, t.DisciplinaId)).ToList());
    }

    // Mesma regra da alocação de professor: disciplina sem etapas vinculadas vale para qualquer etapa.
    private async Task<IReadOnlyList<Disciplina>> DisciplinasDaTurmaAsync(Turma turma)
        => (await _disciplinaRepository.GetAllAsync())
            .Where(d => d.Ativa && (d.EtapasEnsino.Count == 0 || d.EtapasEnsino.Any(e => e.EtapaEnsinoId == turma.EtapaEnsinoId)))
            .ToList();

    private static HorarioResult<HorarioTurmaResponse> NaoEncontrada()
        => new(default, "Turma não encontrada.", HorarioResultError.NotFound);

    private static HorarioResult<HorarioTurmaResponse> Invalida(string mensagem)
        => new(default, mensagem, HorarioResultError.Validation);
}
