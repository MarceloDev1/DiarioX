using System.Net.Mail;
using System.Text.RegularExpressions;
using DiarioX.Server.Application.DTOs.Escolas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class EscolaService : IEscolaService
{
    private static readonly Regex NonDigits = new("\\D", RegexOptions.Compiled);

    private readonly IEscolaRepository _escolaRepository;

    public EscolaService(IEscolaRepository escolaRepository)
    {
        _escolaRepository = escolaRepository;
    }

    public async Task<IEnumerable<EscolaResponse>> GetAllAsync()
    {
        var escolas = await _escolaRepository.GetAllAsync();
        return escolas.Select(MapToResponse);
    }

    public async Task<EscolaResponse?> GetByIdAsync(int id)
    {
        var escola = await _escolaRepository.GetByIdAsync(id);
        return escola is null ? null : MapToResponse(escola);
    }

    public async Task<EscolaCommandResult> CreateAsync(EscolaRequest request)
    {
        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, null);
        if (!validation.Success)
            return validation;

        var escola = new Escola
        {
            CodigoInep = normalized.CodigoInep,
            Nome = normalized.Nome,
            Cnpj = normalized.Cnpj,
            Telefone = normalized.Telefone,
            EmailInstitucional = normalized.EmailInstitucional,
            Municipio = normalized.Municipio,
            EnderecoCompleto = normalized.EnderecoCompleto,
            Status = normalized.Status,
            DiretorId = normalized.DiretorId,
        };

        var created = await _escolaRepository.AddAsync(escola);
        return new EscolaCommandResult(true, "Escola cadastrada com sucesso.", MapToResponse(created));
    }

    public async Task<EscolaCommandResult> UpdateAsync(int id, EscolaRequest request)
    {
        var escola = await _escolaRepository.GetByIdAsync(id);
        if (escola is null)
            return new EscolaCommandResult(false, "Escola nao encontrada.", Error: EscolaResultError.NotFound);

        var normalized = NormalizeRequest(request);
        var validation = await ValidateRequestAsync(normalized, id);
        if (!validation.Success)
            return validation;

        escola.CodigoInep = normalized.CodigoInep;
        escola.Nome = normalized.Nome;
        escola.Cnpj = normalized.Cnpj;
        escola.Telefone = normalized.Telefone;
        escola.EmailInstitucional = normalized.EmailInstitucional;
        escola.Municipio = normalized.Municipio;
        escola.EnderecoCompleto = normalized.EnderecoCompleto;
        escola.Status = normalized.Status;
        escola.DiretorId = normalized.DiretorId;

        await _escolaRepository.UpdateAsync(escola);
        var updated = await _escolaRepository.GetByIdAsync(id);

        return new EscolaCommandResult(true, "Escola atualizada com sucesso.", MapToResponse(updated!));
    }

    public async Task<EscolaCommandResult> DeleteAsync(int id)
    {
        var escola = await _escolaRepository.GetByIdAsync(id);
        if (escola is null)
            return new EscolaCommandResult(false, "Escola nao encontrada.", Error: EscolaResultError.NotFound);

        await _escolaRepository.DeleteAsync(id);
        return new EscolaCommandResult(true, "Escola removida com sucesso.");
    }

    private async Task<EscolaCommandResult> ValidateRequestAsync(EscolaRequest request, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoInep))
            return Invalid("Codigo INEP obrigatorio.");

        if (string.IsNullOrWhiteSpace(request.Nome))
            return Invalid("Nome da escola obrigatorio.");

        if (string.IsNullOrWhiteSpace(request.Cnpj) || request.Cnpj.Length != 14)
            return Invalid("CNPJ deve conter 14 digitos.");

        if (string.IsNullOrWhiteSpace(request.Telefone))
            return Invalid("Telefone obrigatorio.");

        if (!IsValidEmail(request.EmailInstitucional))
            return Invalid("E-mail institucional invalido.");

        if (string.IsNullOrWhiteSpace(request.Municipio))
            return Invalid("Municipio obrigatorio.");

        if (string.IsNullOrWhiteSpace(request.EnderecoCompleto))
            return Invalid("Endereco completo obrigatorio.");

        if (request.Status != Escola.StatusAtivo && request.Status != Escola.StatusInativo)
            return Invalid("Status invalido. Valores permitidos: ATIVO ou INATIVO.");

        var codigoExists = await _escolaRepository.ExistsByCodigoInepAsync(request.CodigoInep, excludeId);
        if (codigoExists)
        {
            return new EscolaCommandResult(
                false,
                "Ja existe uma escola com este codigo INEP.",
                Error: EscolaResultError.Conflict);
        }

        return new EscolaCommandResult(true, string.Empty);
    }

    private static EscolaRequest NormalizeRequest(EscolaRequest request)
    {
        var diretorId = request.DiretorId.HasValue && request.DiretorId.Value > 0
            ? request.DiretorId
            : null;

        return new EscolaRequest
        {
            CodigoInep = request.CodigoInep.Trim(),
            Nome = request.Nome.Trim(),
            Cnpj = NonDigits.Replace(request.Cnpj ?? string.Empty, string.Empty),
            Telefone = request.Telefone.Trim(),
            EmailInstitucional = request.EmailInstitucional.Trim().ToLowerInvariant(),
            Municipio = request.Municipio.Trim(),
            EnderecoCompleto = request.EnderecoCompleto.Trim(),
            Status = (request.Status ?? string.Empty).Trim().ToUpperInvariant(),
            DiretorId = diretorId,
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static EscolaCommandResult Invalid(string message)
        => new(false, message, Error: EscolaResultError.Validation);

    private static EscolaResponse MapToResponse(Escola escola)
        => new(
            escola.Id,
            escola.CodigoInep,
            escola.Nome,
            escola.Cnpj,
            escola.Telefone,
            escola.EmailInstitucional,
            escola.Municipio,
            escola.EnderecoCompleto,
            escola.Status,
            escola.DiretorId
        );
}
