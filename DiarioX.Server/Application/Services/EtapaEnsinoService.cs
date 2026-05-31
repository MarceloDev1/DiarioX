using DiarioX.Server.Application.DTOs.EtapasEnsino;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class EtapaEnsinoService : IEtapaEnsinoService
{
    private readonly IEtapaEnsinoRepository _repository;
    private readonly IModalidadeEnsinoRepository _modalidadeRepository;

    public EtapaEnsinoService(IEtapaEnsinoRepository repository, IModalidadeEnsinoRepository modalidadeRepository)
    {
        _repository = repository;
        _modalidadeRepository = modalidadeRepository;
    }

    public async Task<IEnumerable<EtapaEnsinoResponse>> GetAllAsync()
    {
        var etapas = await _repository.GetAllAsync();
        return etapas.Select(MapToResponse);
    }

    public async Task<EtapaEnsinoResponse?> GetByIdAsync(int id)
    {
        var etapa = await _repository.GetByIdAsync(id);
        return etapa is null ? null : MapToResponse(etapa);
    }

    public async Task<EtapaEnsinoCommandResult> CreateAsync(EtapaEnsinoRequest request)
    {
        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, null);
        if (!validation.Success)
            return validation;

        var etapa = new EtapaEnsino
        {
            ModalidadeEnsinoId = normalized.ModalidadeEnsinoId,
            Nome = normalized.Nome,
            Sigla = normalized.Sigla,
            OrdemCronologica = normalized.OrdemCronologica,
            IdadeRecomendada = normalized.IdadeRecomendada,
        };

        var created = await _repository.AddAsync(etapa);
        var createdWithNav = await _repository.GetByIdAsync(created.Id);
        return new EtapaEnsinoCommandResult(true, "Etapa de ensino cadastrada com sucesso.", MapToResponse(createdWithNav!));
    }

    public async Task<EtapaEnsinoCommandResult> UpdateAsync(int id, EtapaEnsinoRequest request)
    {
        var etapa = await _repository.GetByIdAsync(id);
        if (etapa is null)
            return new EtapaEnsinoCommandResult(false, "Etapa de ensino não encontrada.", Error: EtapaEnsinoResultError.NotFound);

        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, id);
        if (!validation.Success)
            return validation;

        etapa.ModalidadeEnsinoId = normalized.ModalidadeEnsinoId;
        etapa.Nome = normalized.Nome;
        etapa.Sigla = normalized.Sigla;
        etapa.OrdemCronologica = normalized.OrdemCronologica;
        etapa.IdadeRecomendada = normalized.IdadeRecomendada;
        etapa.ModalidadeEnsino = null!;

        await _repository.UpdateAsync(etapa);

        var updated = await _repository.GetByIdAsync(id);
        return new EtapaEnsinoCommandResult(true, "Etapa de ensino atualizada com sucesso.", MapToResponse(updated!));
    }

    public async Task<EtapaEnsinoCommandResult> DeleteAsync(int id)
    {
        var etapa = await _repository.GetByIdAsync(id);
        if (etapa is null)
            return new EtapaEnsinoCommandResult(false, "Etapa de ensino não encontrada.", Error: EtapaEnsinoResultError.NotFound);

        await _repository.DeleteAsync(id);
        return new EtapaEnsinoCommandResult(true, "Etapa de ensino removida com sucesso.");
    }

    private async Task<EtapaEnsinoCommandResult> ValidateRequestAsync(EtapaEnsinoRequest request, int? excludeId)
    {
        if (request.ModalidadeEnsinoId <= 0)
            return Invalid("Modalidade de ensino obrigatória.");

        if (string.IsNullOrWhiteSpace(request.Nome))
            return Invalid("Nome obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Sigla))
            return Invalid("Sigla obrigatória.");

        if (request.OrdemCronologica <= 0)
            return Invalid("Ordem cronológica deve ser maior que zero.");

        if (request.IdadeRecomendada.HasValue && request.IdadeRecomendada.Value <= 0)
            return Invalid("Idade recomendada deve ser maior que zero.");

        var modalidade = await _modalidadeRepository.GetByIdAsync(request.ModalidadeEnsinoId);
        if (modalidade is null)
            return new EtapaEnsinoCommandResult(false, "Modalidade de ensino não encontrada.", Error: EtapaEnsinoResultError.NotFound);

        if (await _repository.ExistsByNomeInModalidadeAsync(request.Nome, request.ModalidadeEnsinoId, excludeId))
            return new EtapaEnsinoCommandResult(false, "Já existe uma etapa com este nome nesta modalidade.", Error: EtapaEnsinoResultError.Conflict);

        if (await _repository.ExistsBySiglaAsync(request.Sigla, excludeId))
            return new EtapaEnsinoCommandResult(false, "Já existe uma etapa com esta sigla.", Error: EtapaEnsinoResultError.Conflict);

        if (await _repository.ExistsByOrdemInModalidadeAsync(request.OrdemCronologica, request.ModalidadeEnsinoId, excludeId))
            return new EtapaEnsinoCommandResult(false, "Já existe uma etapa com esta ordem cronológica nesta modalidade.", Error: EtapaEnsinoResultError.Conflict);

        return new EtapaEnsinoCommandResult(true, string.Empty);
    }

    private static EtapaEnsinoRequest NormalizeRequest(EtapaEnsinoRequest request)
    {
        return new EtapaEnsinoRequest
        {
            ModalidadeEnsinoId = request.ModalidadeEnsinoId,
            Nome = request.Nome.Trim(),
            Sigla = request.Sigla.Trim().ToUpperInvariant(),
            OrdemCronologica = request.OrdemCronologica,
            IdadeRecomendada = request.IdadeRecomendada,
        };
    }

    private static EtapaEnsinoCommandResult Invalid(string message)
        => new(false, message, Error: EtapaEnsinoResultError.Validation);

    private static EtapaEnsinoResponse MapToResponse(EtapaEnsino e)
        => new(e.Id, e.ModalidadeEnsinoId, e.ModalidadeEnsino.Nome, e.Nome, e.Sigla, e.OrdemCronologica, e.IdadeRecomendada);
}
