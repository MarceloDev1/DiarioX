using DiarioX.Server.Application.DTOs.Disciplinas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class DisciplinaService : IDisciplinaService
{
    private readonly IDisciplinaRepository _repository;
    private readonly IEtapaEnsinoRepository _etapaRepository;

    public DisciplinaService(IDisciplinaRepository repository, IEtapaEnsinoRepository etapaRepository)
    {
        _repository = repository;
        _etapaRepository = etapaRepository;
    }

    public async Task<IEnumerable<DisciplinaResponse>> GetAllAsync()
    {
        var disciplinas = await _repository.GetAllAsync();
        return disciplinas.Select(MapToResponse);
    }

    public async Task<DisciplinaResponse?> GetByIdAsync(int id)
    {
        var disciplina = await _repository.GetByIdAsync(id);
        return disciplina is null ? null : MapToResponse(disciplina);
    }

    public async Task<DisciplinaCommandResult> CreateAsync(DisciplinaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return new(false, "Nome é obrigatório.", Error: DisciplinaResultError.Validation);

        if (string.IsNullOrWhiteSpace(request.Codigo))
            return new(false, "Código é obrigatório.", Error: DisciplinaResultError.Validation);

        if (await _repository.ExistsByNomeAsync(request.Nome))
            return new(false, "Já existe uma disciplina com este nome.", Error: DisciplinaResultError.Conflict);

        if (await _repository.ExistsByCodigoAsync(request.Codigo))
            return new(false, "Já existe uma disciplina com este código.", Error: DisciplinaResultError.Conflict);

        if (!await EtapasExistemAsync(request.EtapasEnsinoIds))
            return new(false, "Etapa de ensino não encontrada.", Error: DisciplinaResultError.Validation);

        var disciplina = new Disciplina
        {
            Nome = request.Nome.Trim(),
            Codigo = request.Codigo.Trim(),
            Descricao = request.Descricao?.Trim() ?? string.Empty,
            EtapasEnsino = request.EtapasEnsinoIds.Select(id => new DisciplinaEtapaEnsino
            {
                EtapaEnsinoId = id
            }).ToList()
        };

        var created = await _repository.AddAsync(disciplina);
        return new(true, "Disciplina criada com sucesso.", MapToResponse(created));
    }

    public async Task<DisciplinaCommandResult> UpdateAsync(int id, DisciplinaRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return new(false, "Disciplina não encontrada.", Error: DisciplinaResultError.NotFound);

        if (string.IsNullOrWhiteSpace(request.Nome))
            return new(false, "Nome é obrigatório.", Error: DisciplinaResultError.Validation);

        if (string.IsNullOrWhiteSpace(request.Codigo))
            return new(false, "Código é obrigatório.", Error: DisciplinaResultError.Validation);

        if (await _repository.ExistsByNomeAsync(request.Nome, id))
            return new(false, "Já existe uma disciplina com este nome.", Error: DisciplinaResultError.Conflict);

        if (await _repository.ExistsByCodigoAsync(request.Codigo, id))
            return new(false, "Já existe uma disciplina com este código.", Error: DisciplinaResultError.Conflict);

        if (!await EtapasExistemAsync(request.EtapasEnsinoIds))
            return new(false, "Etapa de ensino não encontrada.", Error: DisciplinaResultError.Validation);

        var disciplina = new Disciplina
        {
            Id = id,
            Nome = request.Nome.Trim(),
            Codigo = request.Codigo.Trim(),
            Descricao = request.Descricao?.Trim() ?? string.Empty,
            EtapasEnsino = request.EtapasEnsinoIds.Select(etapaId => new DisciplinaEtapaEnsino
            {
                EtapaEnsinoId = etapaId
            }).ToList()
        };

        await _repository.UpdateAsync(disciplina);
        var updated = await _repository.GetByIdAsync(id);
        return new(true, "Disciplina atualizada com sucesso.", MapToResponse(updated!));
    }

    public async Task<DisciplinaCommandResult> DeleteAsync(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return new(false, "Disciplina não encontrada.", Error: DisciplinaResultError.NotFound);

        await _repository.DeleteAsync(id);
        return new(true, "Disciplina excluída com sucesso.");
    }

    // O repositório só enxerga etapas da instituição atual, o que impede vincular
    // a disciplina a uma etapa de outra instituição pelo id.
    private async Task<bool> EtapasExistemAsync(IEnumerable<int> etapaIds)
    {
        foreach (var etapaId in etapaIds.Distinct())
        {
            if (await _etapaRepository.GetByIdAsync(etapaId) is null)
                return false;
        }

        return true;
    }

    private static DisciplinaResponse MapToResponse(Disciplina d) => new(
        d.Id,
        d.Nome,
        d.Codigo,
        d.Descricao,
        d.Ativa,
        d.EtapasEnsino.Select(de => new DisciplinaEtapaEnsinoResponse(
            de.EtapaEnsinoId,
            de.EtapaEnsino?.Nome ?? string.Empty
        )).ToList()
    );
}
