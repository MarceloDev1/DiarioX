using DiarioX.Server.Application.DTOs.AnosLetivos;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class AnoLetivoService : IAnoLetivoService
{
    private static readonly string[] TiposValidos = [AnoLetivo.TipoBimestral, AnoLetivo.TipoTrimestral, AnoLetivo.TipoSemestral];

    private static readonly Dictionary<string, int> QuantidadePeriodos = new()
    {
        [AnoLetivo.TipoBimestral] = 4,
        [AnoLetivo.TipoTrimestral] = 3,
        [AnoLetivo.TipoSemestral] = 2,
    };

    private readonly IAnoLetivoRepository _repository;

    public AnoLetivoService(IAnoLetivoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AnoLetivoResponse>> GetAllAsync()
    {
        var anos = await _repository.GetAllAsync();
        return anos.Select(MapToResponse);
    }

    public async Task<AnoLetivoResponse?> GetByIdAsync(int id)
    {
        var ano = await _repository.GetByIdAsync(id);
        return ano is null ? null : MapToResponse(ano);
    }

    public async Task<AnoLetivoCommandResult> CreateAsync(AnoLetivoRequest request)
    {
        var validation = await ValidateRequestAsync(request, null);
        if (!validation.Success) return validation;

        var anoLetivo = new AnoLetivo
        {
            AnoReferencia = request.AnoReferencia,
            DataInicio = request.DataInicio,
            DataTermino = request.DataTermino,
            TipoPeriodo = request.TipoPeriodo,
            Periodos = request.Periodos.Select(p => new PeriodoAvaliativo
            {
                Nome = p.Nome.Trim(),
                Numero = p.Numero,
                DataInicio = p.DataInicio,
                DataTermino = p.DataTermino,
            }).ToList(),
        };

        var created = await _repository.AddAsync(anoLetivo);
        var createdWithNav = await _repository.GetByIdAsync(created.Id);
        return new AnoLetivoCommandResult(true, "Ano letivo cadastrado com sucesso.", MapToResponse(createdWithNav!));
    }

    public async Task<AnoLetivoCommandResult> UpdateAsync(int id, AnoLetivoRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return new AnoLetivoCommandResult(false, "Ano letivo não encontrado.", Error: AnoLetivoResultError.NotFound);

        var validation = await ValidateRequestAsync(request, id);
        if (!validation.Success) return validation;

        var entity = new AnoLetivo
        {
            Id = id,
            AnoReferencia = request.AnoReferencia,
            DataInicio = request.DataInicio,
            DataTermino = request.DataTermino,
            TipoPeriodo = request.TipoPeriodo,
            Periodos = request.Periodos.Select(p => new PeriodoAvaliativo
            {
                Nome = p.Nome.Trim(),
                Numero = p.Numero,
                DataInicio = p.DataInicio,
                DataTermino = p.DataTermino,
            }).ToList(),
        };

        await _repository.UpdateAsync(entity);
        var updated = await _repository.GetByIdAsync(id);
        return new AnoLetivoCommandResult(true, "Ano letivo atualizado com sucesso.", MapToResponse(updated!));
    }

    public async Task<AnoLetivoCommandResult> DeleteAsync(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return new AnoLetivoCommandResult(false, "Ano letivo não encontrado.", Error: AnoLetivoResultError.NotFound);

        var entity = new AnoLetivo { Id = id };
        await _repository.DeleteAsync(entity);
        return new AnoLetivoCommandResult(true, "Ano letivo excluído com sucesso.");
    }

    private async Task<AnoLetivoCommandResult> ValidateRequestAsync(AnoLetivoRequest request, int? excludeId)
    {
        if (request.AnoReferencia < 2000 || request.AnoReferencia > 2100)
            return Invalid("Ano de referência inválido. Deve estar entre 2000 e 2100.");

        if (request.DataInicio >= request.DataTermino)
            return Invalid("A data de início deve ser anterior à data de término do ano letivo.");

        if (!TiposValidos.Contains(request.TipoPeriodo))
            return Invalid("Tipo de período inválido. Use BIMESTRAL, TRIMESTRAL ou SEMESTRAL.");

        var qtdEsperada = QuantidadePeriodos[request.TipoPeriodo];
        if (request.Periodos.Count != qtdEsperada)
            return Invalid($"O tipo {request.TipoPeriodo} requer exatamente {qtdEsperada} período(s).");

        for (var i = 0; i < request.Periodos.Count; i++)
        {
            var p = request.Periodos[i];
            if (string.IsNullOrWhiteSpace(p.Nome))
                return Invalid($"O nome do período {i + 1} é obrigatório.");

            if (p.DataInicio >= p.DataTermino)
                return Invalid($"No período {i + 1}, a data de início deve ser anterior à data de término.");

            if (p.DataInicio < request.DataInicio || p.DataTermino > request.DataTermino)
                return Invalid($"O período {i + 1} deve estar dentro do intervalo do ano letivo.");
        }

        if (await _repository.ExistsByAnoReferenciaAsync(request.AnoReferencia, excludeId))
            return new AnoLetivoCommandResult(false, $"Já existe um ano letivo cadastrado para {request.AnoReferencia}.",
                Error: AnoLetivoResultError.Conflict);

        return new AnoLetivoCommandResult(true, string.Empty);
    }

    private static AnoLetivoCommandResult Invalid(string message) =>
        new(false, message, Error: AnoLetivoResultError.Validation);

    private static AnoLetivoResponse MapToResponse(AnoLetivo a) =>
        new(
            a.Id,
            a.AnoReferencia,
            a.DataInicio,
            a.DataTermino,
            a.TipoPeriodo,
            a.Periodos.OrderBy(p => p.Numero).Select(p =>
                new PeriodoAvaliativoResponse(p.Id, p.AnoLetivoId, p.Nome, p.Numero, p.DataInicio, p.DataTermino)
            ).ToList()
        );
}
