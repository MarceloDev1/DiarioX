using DiarioX.Server.Application.DTOs.Alocacoes;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class ProfessorAlocacaoService : IProfessorAlocacaoService
{
    private readonly IProfessorAlocacaoRepository _alocacaoRepository;
    private readonly IProfessorRepository _professorRepository;
    private readonly ITurmaRepository _turmaRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;

    public ProfessorAlocacaoService(
        IProfessorAlocacaoRepository alocacaoRepository,
        IProfessorRepository professorRepository,
        ITurmaRepository turmaRepository,
        IDisciplinaRepository disciplinaRepository)
    {
        _alocacaoRepository = alocacaoRepository;
        _professorRepository = professorRepository;
        _turmaRepository = turmaRepository;
        _disciplinaRepository = disciplinaRepository;
    }

    public async Task<IEnumerable<ProfessorAlocacaoResponse>> GetByProfessorAsync(int professorId)
        => (await _alocacaoRepository.GetByProfessorIdAsync(professorId)).Select(Map);

    public async Task<IEnumerable<ProfessorAlocacaoDisponibilidadeResponse>> GetDisponiveisAsync(int professorId)
    {
        var professor = await _professorRepository.GetByIdAsync(professorId);
        if (professor is null)
            return [];

        var escolaIds = professor.ProfessorEscolas.Select(x => x.EscolaId).ToHashSet();
        var turmas = (await _turmaRepository.GetAllAsync())
            .Where(t => escolaIds.Contains(t.EscolaId) && t.Status == Turma.StatusAtivo)
            .ToList();
        var habilitacoes = professor.ProfessorDisciplinas.Select(x => x.DisciplinaId).ToHashSet();
        var disciplinas = (await _disciplinaRepository.GetAllAsync())
            .Where(d => d.Ativa && habilitacoes.Contains(d.Id))
            .ToList();
        var alocacoes = (await _alocacaoRepository.GetAtivasAsync()).ToList();

        return turmas
            .Select(turma => new ProfessorAlocacaoDisponibilidadeResponse(
                turma.Id,
                turma.NomeCompleto,
                turma.AnoLetivoId,
                turma.AnoLetivo.AnoReferencia,
                turma.Turno,
                disciplinas
                    .Where(d => d.EtapasEnsino.Count == 0 || d.EtapasEnsino.Any(e => e.EtapaEnsinoId == turma.EtapaEnsinoId))
                    .Select(d =>
                    {
                        var atual = alocacoes.FirstOrDefault(a => a.TurmaId == turma.Id && a.DisciplinaId == d.Id);
                        return new ProfessorAlocacaoDisciplinaResponse(d.Id, d.Nome, atual?.Id, atual?.Professor.Nome);
                    })
                    .ToList()))
            .Where(x => x.Disciplinas.Count > 0);
    }

    public async Task<ProfessorAlocacaoCommandResult> CreateAsync(ProfessorAlocacaoRequest request)
    {
        if (request.Itens is null || request.Itens.Count == 0)
            return Invalid("Selecione ao menos uma turma e disciplina.");

        var professor = await _professorRepository.GetByIdAsync(request.ProfessorId);
        if (professor is null)
            return NotFound("Professor não encontrado.");

        var turmas = (await _turmaRepository.GetAllAsync()).ToDictionary(x => x.Id);
        var disciplinas = (await _disciplinaRepository.GetAllAsync()).ToDictionary(x => x.Id);
        var existentes = (await _alocacaoRepository.GetAtivasAsync()).ToList();
        var habilitacoes = professor.ProfessorDisciplinas.Select(x => x.DisciplinaId).ToHashSet();
        var escolaIds = professor.ProfessorEscolas.Select(x => x.EscolaId).ToHashSet();
        var novos = new List<ProfessorAlocacao>();
        var removerIds = new List<int>();

        foreach (var item in request.Itens)
        {
            if (!turmas.TryGetValue(item.TurmaId, out var turma) || turma.Status != Turma.StatusAtivo || !escolaIds.Contains(turma.EscolaId))
                return Invalid("A turma selecionada não está disponível para este professor.");

            if (!disciplinas.TryGetValue(item.DisciplinaId, out var disciplina) || !disciplina.Ativa || !habilitacoes.Contains(item.DisciplinaId))
                return Invalid("O professor não possui habilitação para a disciplina selecionada.");

            if (disciplina.EtapasEnsino.Count > 0 && !disciplina.EtapasEnsino.Any(x => x.EtapaEnsinoId == turma.EtapaEnsinoId))
                return Invalid("A disciplina selecionada não pertence à etapa de ensino da turma.");

            if (novos.Any(x => x.TurmaId == item.TurmaId && x.DisciplinaId == item.DisciplinaId))
                return Conflict("A mesma disciplina foi selecionada mais de uma vez para esta turma.");

            var ocupada = existentes.FirstOrDefault(x => x.TurmaId == item.TurmaId && x.DisciplinaId == item.DisciplinaId);
            if (ocupada is not null)
            {
                if (ocupada.ProfessorId == professor.Id)
                    return Conflict("Este professor já está alocado nesta turma e disciplina.");

                if (item.SubstituirAlocacaoId != ocupada.Id)
                    return Conflict($"Esta disciplina já possui o(a) Professor(a) {ocupada.Professor.Nome} alocado nesta turma. Informe a substituição para continuar.");

                removerIds.Add(ocupada.Id);
            }
            else if (item.SubstituirAlocacaoId.HasValue)
            {
                return Invalid("A alocação indicada para substituição não corresponde à turma e disciplina selecionadas.");
            }

            novos.Add(new ProfessorAlocacao
            {
                ProfessorId = professor.Id,
                TurmaId = turma.Id,
                DisciplinaId = disciplina.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _alocacaoRepository.SaveAsync(novos, removerIds);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex.GetType().Name == "DbUpdateException")
        {
            return Conflict("A alocação não pôde ser confirmada porque a vaga foi ocupada por outro usuário.");
        }

        return new ProfessorAlocacaoCommandResult(
            true,
            "Enturmação realizada com sucesso!",
            await GetByProfessorAsync(professor.Id));
    }

    public async Task<ProfessorAlocacaoCommandResult> DeleteAsync(int id)
    {
        var alocacao = await _alocacaoRepository.GetByIdAsync(id);
        if (alocacao is null)
            return NotFound("Alocação não encontrada.");

        await _alocacaoRepository.DeleteAsync(alocacao);
        return new ProfessorAlocacaoCommandResult(true, "Alocação removida com sucesso!");
    }

    private static ProfessorAlocacaoResponse Map(ProfessorAlocacao item)
        => new(item.Id, item.ProfessorId, item.Professor.Nome, item.TurmaId, item.Turma.NomeCompleto,
            item.Turma.AnoLetivoId, item.Turma.AnoLetivo.AnoReferencia, item.Turma.Turno,
            item.DisciplinaId, item.Disciplina.Nome, item.Ativa);

    private static ProfessorAlocacaoCommandResult Invalid(string message)
        => new(false, message, Error: ProfessorAlocacaoResultError.Validation);

    private static ProfessorAlocacaoCommandResult NotFound(string message)
        => new(false, message, Error: ProfessorAlocacaoResultError.NotFound);

    private static ProfessorAlocacaoCommandResult Conflict(string message)
        => new(false, message, Error: ProfessorAlocacaoResultError.Conflict);
}