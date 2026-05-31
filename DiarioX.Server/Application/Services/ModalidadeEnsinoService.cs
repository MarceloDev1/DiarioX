using DiarioX.Server.Application.DTOs.ModalidadesEnsino;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class ModalidadeEnsinoService : IModalidadeEnsinoService
{
    private readonly IModalidadeEnsinoRepository _repository;

    public ModalidadeEnsinoService(IModalidadeEnsinoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ModalidadeEnsinoResponse>> GetAllAsync()
    {
        var modalidades = await _repository.GetAllAsync();
        return modalidades.Select(MapToResponse);
    }

    public async Task<ModalidadeEnsinoResponse?> GetByIdAsync(int id)
    {
        var modalidade = await _repository.GetByIdAsync(id);
        return modalidade is null ? null : MapToResponse(modalidade);
    }

    public async Task<ModalidadeEnsinoCommandResult> CreateAsync(ModalidadeEnsinoRequest request)
    {
        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, null);
        if (!validation.Success)
            return validation;

        var modalidade = new ModalidadeEnsino
        {
            Nome = normalized.Nome,
            Sigla = normalized.Sigla,
            CodigoMec = normalized.CodigoMec,
            Descricao = normalized.Descricao,
            Status = normalized.Status,
        };

        var created = await _repository.AddAsync(modalidade);
        return new ModalidadeEnsinoCommandResult(true, "Modalidade de ensino cadastrada com sucesso.", MapToResponse(created));
    }

    public async Task<ModalidadeEnsinoCommandResult> UpdateAsync(int id, ModalidadeEnsinoRequest request)
    {
        var modalidade = await _repository.GetByIdAsync(id);
        if (modalidade is null)
            return new ModalidadeEnsinoCommandResult(false, "Modalidade de ensino nao encontrada.", Error: ModalidadeEnsinoResultError.NotFound);

        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, id);
        if (!validation.Success)
            return validation;

        modalidade.Nome = normalized.Nome;
        modalidade.Sigla = normalized.Sigla;
        modalidade.CodigoMec = normalized.CodigoMec;
        modalidade.Descricao = normalized.Descricao;
        modalidade.Status = normalized.Status;

        await _repository.UpdateAsync(modalidade);

        var updated = await _repository.GetByIdAsync(id);
        return new ModalidadeEnsinoCommandResult(true, "Modalidade de ensino atualizada com sucesso.", MapToResponse(updated!));
    }

    public async Task<ModalidadeEnsinoCommandResult> DeleteAsync(int id)
    {
        var modalidade = await _repository.GetByIdAsync(id);
        if (modalidade is null)
            return new ModalidadeEnsinoCommandResult(false, "Modalidade de ensino nao encontrada.", Error: ModalidadeEnsinoResultError.NotFound);

        await _repository.DeleteAsync(id);
        return new ModalidadeEnsinoCommandResult(true, "Modalidade de ensino removida com sucesso.");
    }

    private async Task<ModalidadeEnsinoCommandResult> ValidateRequestAsync(ModalidadeEnsinoRequest request, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return Invalid("Nome obrigatorio.");

        if (string.IsNullOrWhiteSpace(request.Sigla))
            return Invalid("Sigla obrigatoria.");

        if (string.IsNullOrWhiteSpace(request.Descricao))
            return Invalid("Descricao obrigatoria.");

        if (request.Status != ModalidadeEnsino.StatusAtivo && request.Status != ModalidadeEnsino.StatusInativo)
            return Invalid("Status invalido. Valores permitidos: ATIVO ou INATIVO.");

        if (await _repository.ExistsByNomeAsync(request.Nome, excludeId))
            return new ModalidadeEnsinoCommandResult(false, "Ja existe uma modalidade de ensino com este nome.", Error: ModalidadeEnsinoResultError.Conflict);

        if (await _repository.ExistsBySiglaAsync(request.Sigla, excludeId))
            return new ModalidadeEnsinoCommandResult(false, "Ja existe uma modalidade de ensino com esta sigla.", Error: ModalidadeEnsinoResultError.Conflict);

        return new ModalidadeEnsinoCommandResult(true, string.Empty);
    }

    private static ModalidadeEnsinoRequest NormalizeRequest(ModalidadeEnsinoRequest request)
    {
        return new ModalidadeEnsinoRequest
        {
            Nome = request.Nome.Trim(),
            Sigla = request.Sigla.Trim().ToUpperInvariant(),
            CodigoMec = string.IsNullOrWhiteSpace(request.CodigoMec) ? null : request.CodigoMec.Trim(),
            Descricao = request.Descricao.Trim(),
            Status = (request.Status ?? string.Empty).Trim().ToUpperInvariant(),
        };
    }

    private static ModalidadeEnsinoCommandResult Invalid(string message)
        => new(false, message, Error: ModalidadeEnsinoResultError.Validation);

    private static ModalidadeEnsinoResponse MapToResponse(ModalidadeEnsino m)
        => new(m.Id, m.Nome, m.Sigla, m.CodigoMec, m.Descricao, m.Status);
}
