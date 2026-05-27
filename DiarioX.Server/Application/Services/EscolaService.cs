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
    private readonly IUserRepository _userRepository;
    private readonly IUsuarioPerfilRepository _usuarioPerfilRepository;

    public EscolaService(
        IEscolaRepository escolaRepository,
        IUserRepository userRepository,
        IUsuarioPerfilRepository usuarioPerfilRepository)
    {
        _escolaRepository = escolaRepository;
        _userRepository = userRepository;
        _usuarioPerfilRepository = usuarioPerfilRepository;
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

        int? diretorId = null;
        if (normalized.CpfDiretor is not null)
        {
            var user = await _userRepository.GetByCpfAsync(normalized.CpfDiretor);
            if (user is null)
                return new EscolaCommandResult(false, "Nenhum usuario encontrado com o CPF informado.", Error: EscolaResultError.UserNotFound);
            diretorId = user.Id;
        }

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
            DiretorId = diretorId,
        };

        var created = await _escolaRepository.AddAsync(escola);

        if (diretorId.HasValue)
            await AtualizarEscolaNoPerfilAsync(diretorId.Value, created.Id);

        var reloaded = await _escolaRepository.GetByIdAsync(created.Id);
        return new EscolaCommandResult(true, "Escola cadastrada com sucesso.", MapToResponse(reloaded!));
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

        int? diretorId = null;
        if (normalized.CpfDiretor is not null)
        {
            var user = await _userRepository.GetByCpfAsync(normalized.CpfDiretor);
            if (user is null)
                return new EscolaCommandResult(false, "Nenhum usuario encontrado com o CPF informado.", Error: EscolaResultError.UserNotFound);
            diretorId = user.Id;
        }

        escola.CodigoInep = normalized.CodigoInep;
        escola.Nome = normalized.Nome;
        escola.Cnpj = normalized.Cnpj;
        escola.Telefone = normalized.Telefone;
        escola.EmailInstitucional = normalized.EmailInstitucional;
        escola.Municipio = normalized.Municipio;
        escola.EnderecoCompleto = normalized.EnderecoCompleto;
        escola.Status = normalized.Status;
        escola.DiretorId = diretorId;

        await _escolaRepository.UpdateAsync(escola);

        if (diretorId.HasValue)
            await AtualizarEscolaNoPerfilAsync(diretorId.Value, id);

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

    private async Task AtualizarEscolaNoPerfilAsync(int usuarioId, int escolaId)
    {
        var perfil = await _usuarioPerfilRepository.GetGlobalByUsuarioIdAsync(usuarioId);
        if (perfil is not null)
        {
            perfil.EscolaId = escolaId;
            await _usuarioPerfilRepository.UpdateAsync(perfil);
        }
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

        if (request.CpfDiretor is not null && request.CpfDiretor.Length != 11)
            return Invalid("CPF do diretor deve conter 11 digitos.");

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
        var rawCpf = NonDigits.Replace(request.CpfDiretor ?? string.Empty, string.Empty);

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
            CpfDiretor = rawCpf.Length > 0 ? rawCpf : null,
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
            escola.Diretor?.Cpf
        );
}
