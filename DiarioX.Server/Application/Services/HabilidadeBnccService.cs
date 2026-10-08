using DiarioX.Server.Application.DTOs.Conteudos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class HabilidadeBnccService : IHabilidadeBnccService
{
    private const int MaxCodigo = 20;
    private const int MaxDescricao = 1000;

    private readonly IHabilidadeBnccRepository _repository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IEtapaEnsinoRepository _etapaRepository;

    public HabilidadeBnccService(
        IHabilidadeBnccRepository repository,
        IDisciplinaRepository disciplinaRepository,
        IEtapaEnsinoRepository etapaRepository)
    {
        _repository = repository;
        _disciplinaRepository = disciplinaRepository;
        _etapaRepository = etapaRepository;
    }

    public async Task<IEnumerable<HabilidadeBnccResponse>> GetAllAsync()
        => (await _repository.GetAllAsync()).Select(MapToResponse).ToList();

    public async Task<HabilidadeBnccResponse?> GetByIdAsync(int id)
    {
        var habilidade = await _repository.GetByIdAsync(id);
        return habilidade is null ? null : MapToResponse(habilidade);
    }

    public async Task<HabilidadeBnccCommandResult> CreateAsync(HabilidadeBnccRequest request)
    {
        var erro = await ValidarAsync(request, excludeId: null);
        if (erro is not null)
            return erro;

        var criada = await _repository.AddAsync(MapToEntity(new HabilidadeBncc(), request));
        return new(true, "Habilidade cadastrada com sucesso.", MapToResponse(criada));
    }

    public async Task<HabilidadeBnccCommandResult> UpdateAsync(int id, HabilidadeBnccRequest request)
    {
        if (await _repository.GetByIdAsync(id) is null)
            return NotFound();

        var erro = await ValidarAsync(request, id);
        if (erro is not null)
            return erro;

        var habilidade = MapToEntity(new HabilidadeBncc { Id = id }, request);
        await _repository.UpdateAsync(habilidade);

        var atualizada = await _repository.GetByIdAsync(id);
        return new(true, "Habilidade atualizada com sucesso.", MapToResponse(atualizada!));
    }

    public async Task<HabilidadeBnccCommandResult> DeleteAsync(int id)
    {
        if (await _repository.GetByIdAsync(id) is null)
            return NotFound();

        if (await _repository.EmUsoAsync(id))
        {
            return new(false,
                "A habilidade já foi usada em conteúdos ministrados e não pode ser excluída. Inative-a para que deixe de ser sugerida.",
                Error: HabilidadeBnccResultError.Conflict);
        }

        await _repository.DeleteAsync(id);
        return new(true, "Habilidade excluída com sucesso.");
    }

    private async Task<HabilidadeBnccCommandResult?> ValidarAsync(HabilidadeBnccRequest request, int? excludeId)
    {
        var codigo = request.Codigo?.Trim() ?? string.Empty;
        if (codigo.Length == 0)
            return Invalid("Código é obrigatório.");
        if (codigo.Length > MaxCodigo)
            return Invalid($"O código deve ter no máximo {MaxCodigo} caracteres.");

        var descricao = request.Descricao?.Trim() ?? string.Empty;
        if (descricao.Length == 0)
            return Invalid("Descrição é obrigatória.");
        if (descricao.Length > MaxDescricao)
            return Invalid($"A descrição deve ter no máximo {MaxDescricao} caracteres.");

        if (await _disciplinaRepository.GetByIdAsync(request.DisciplinaId) is null)
            return Invalid("Disciplina não encontrada.");

        var etapas = request.EtapasEnsinoIds?.Distinct().ToList() ?? [];
        if (etapas.Count == 0)
            return Invalid("Selecione pelo menos uma etapa de ensino.");

        // O repositório só enxerga etapas da instituição atual.
        foreach (var etapaId in etapas)
        {
            if (await _etapaRepository.GetByIdAsync(etapaId) is null)
                return Invalid("Etapa de ensino não encontrada.");
        }

        if (await _repository.ExistsByCodigoAsync(codigo, excludeId))
            return new(false, "Já existe uma habilidade com este código.", Error: HabilidadeBnccResultError.Conflict);

        return null;
    }

    private static HabilidadeBncc MapToEntity(HabilidadeBncc habilidade, HabilidadeBnccRequest request)
    {
        habilidade.Codigo = request.Codigo.Trim().ToUpperInvariant();
        habilidade.Descricao = request.Descricao.Trim();
        habilidade.DisciplinaId = request.DisciplinaId;
        habilidade.Ativa = request.Ativa;
        habilidade.EtapasEnsino = request.EtapasEnsinoIds.Distinct()
            .Select(id => new HabilidadeBnccEtapaEnsino { EtapaEnsinoId = id })
            .ToList();
        return habilidade;
    }

    private static HabilidadeBnccResponse MapToResponse(HabilidadeBncc h) => new(
        h.Id,
        h.Codigo,
        h.Descricao,
        h.DisciplinaId,
        h.Disciplina?.Nome ?? string.Empty,
        h.Ativa,
        h.EtapasEnsino
            .Select(e => new HabilidadeBnccEtapaResponse(e.EtapaEnsinoId, e.EtapaEnsino?.Nome ?? string.Empty))
            .OrderBy(e => e.Nome)
            .ToList());

    private static HabilidadeBnccCommandResult Invalid(string message)
        => new(false, message, Error: HabilidadeBnccResultError.Validation);

    private static HabilidadeBnccCommandResult NotFound()
        => new(false, "Habilidade não encontrada.", Error: HabilidadeBnccResultError.NotFound);
}
