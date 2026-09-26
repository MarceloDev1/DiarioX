using System.Net.Mail;
using System.Text.RegularExpressions;
using DiarioX.Server.Application.DTOs.Users;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DiarioX.Server.Application.Services;

public class UserService : IUserService
{
    private static readonly Regex NonDigits = new("\\D", RegexOptions.Compiled);
    private static readonly Regex PasswordPolicy = new(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z\\d]).{8,}$",
        RegexOptions.Compiled);

    // Usuários cadastrados aqui sempre pertencem à instituição da requisição;
    // o perfil Administrador fica reservado aos usuários globais.
    private const string AdministradorExclusivoMessage =
        "O perfil Administrador e exclusivo dos administradores globais e nao pode ser atribuido a usuarios da instituicao.";

    private readonly IUserRepository _userRepository;
    private readonly IPerfilRepository _perfilRepository;
    private readonly IUsuarioPerfilRepository _usuarioPerfilRepository;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<UserService> _logger;
    private readonly IEscolaRepository _escolaRepository;
    private readonly ITenantContext _tenantContext;

    public UserService(
        IUserRepository userRepository,
        IPerfilRepository perfilRepository,
        IUsuarioPerfilRepository usuarioPerfilRepository,
        IEmailNotificationService emailNotificationService,
        ILogger<UserService> logger,
        IEscolaRepository escolaRepository,
        ITenantContext tenantContext)
    {
        _userRepository = userRepository;
        _perfilRepository = perfilRepository;
        _usuarioPerfilRepository = usuarioPerfilRepository;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
        _escolaRepository = escolaRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserCommandResult> CreateAsync(UserRequest request)
    {
        var normalized = NormalizeRequest(request);

        var (perfil, perfilError) = await LoadPerfilAsync(normalized);
        if (perfilError is not null)
            return perfilError;

        var validation = await ValidateForCreateAsync(normalized);
        if (!validation.Success)
            return validation;

        if (perfil is not null)
        {
            var escolasError = await ValidateEscolasAsync(perfil, normalized.EscolaIds!, atuais: null);
            if (escolasError is not null)
                return escolasError;
        }

        var user = new User
        {
            Email = normalized.Email,
            Cpf = normalized.Cpf,
            DataNascimento = normalized.DataNascimento,
            Status = normalized.Status,
        };

        if (!string.IsNullOrWhiteSpace(normalized.Senha))
            user.SetPassword(normalized.Senha);

        var created = await _userRepository.AddAsync(user);

        if (perfil is not null)
            await _usuarioPerfilRepository.SubstituirAsync(created.Id, perfil.Id, normalized.EscolaIds!);

        var result = await _userRepository.GetByIdAsync(created.Id);
        var userResponse = MapToResponse(result!);

        // Enviar email de boas-vindas de forma assíncrona
        _ = SendWelcomeEmailAsync(result!.Email, userResponse.Email);

        return new UserCommandResult(true, "Usuario cadastrado com sucesso.", userResponse);
    }

    /// <summary>
    /// Envia email de boas-vindas para o novo usuário.
    /// Executado de forma não-bloqueante para não impactar a criação do usuário.
    /// </summary>
    private async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        try
        {
            await _emailNotificationService.SendWelcomeAsync(toEmail, userName, toEmail);
            _logger.LogInformation("Email de boas-vindas enviado com sucesso para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar email de boas-vindas para {Email}. A conta foi criada normalmente.", toEmail);
            // Não relançar exceção - criação do usuário já foi bem-sucedida
        }
    }

    public async Task<UserCommandResult> UpdateAsync(int id, UserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return new UserCommandResult(false, "Usuario nao encontrado.", Error: UserResultError.NotFound);

        var normalized = NormalizeRequest(request);

        var (perfil, perfilError) = await LoadPerfilAsync(normalized);
        if (perfilError is not null)
            return perfilError;

        var validation = await ValidateForUpdateAsync(normalized, id);
        if (!validation.Success)
            return validation;

        // Perfil ou escolas só são regravados quando mudam.
        var alterarAcesso = perfil is not null && !MesmoAcesso(user, perfil.Id, normalized.EscolaIds!);
        if (alterarAcesso)
        {
            var escolasError = await ValidateEscolasAsync(perfil!, normalized.EscolaIds!, user.UsuariosPerfis.ToList());
            if (escolasError is not null)
                return escolasError;
        }

        user.Email = normalized.Email;
        user.Cpf = normalized.Cpf;
        user.DataNascimento = normalized.DataNascimento;
        user.Status = normalized.Status;

        if (!string.IsNullOrWhiteSpace(normalized.Senha))
            user.SetPassword(normalized.Senha);

        await _userRepository.UpdateAsync(user);

        if (alterarAcesso)
            await _usuarioPerfilRepository.SubstituirAsync(id, perfil!.Id, normalized.EscolaIds!);

        var updated = await _userRepository.GetByIdAsync(id);
        return new UserCommandResult(true, "Usuario atualizado com sucesso.", MapToResponse(updated!));
    }

    public async Task<UserCommandResult> DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return new UserCommandResult(false, "Usuario nao encontrado.", Error: UserResultError.NotFound);

        try
        {
            await _userRepository.DeleteAsync(id);
        }
        catch
        {
            return new UserCommandResult(
                false,
                "Não é possivel remover este usuário pois ele esta associado a outros registros.",
                Error: UserResultError.Conflict);
        }

        return new UserCommandResult(true, "Usuário removido com sucesso.");
    }

    private async Task<(Perfil? Perfil, UserCommandResult? Error)> LoadPerfilAsync(UserRequest request)
    {
        if (!request.PerfilId.HasValue)
        {
            return request.EscolaIds!.Count > 0
                ? (null, Invalid("Selecione o perfil do usuário para definir as escolas de atuação."))
                : (null, null);
        }

        var perfil = await _perfilRepository.GetByIdAsync(request.PerfilId.Value);
        if (perfil is null)
            return (null, Invalid("Perfil nao encontrado."));

        if (IsAdministrador(perfil))
            return (null, Invalid(AdministradorExclusivoMessage));

        return (perfil, null);
    }

    /// <summary>
    /// As escolas precisam existir e estar ao alcance de quem cadastra (o repositório de escolas já
    /// respeita o escopo do usuário logado). Quem atua só em algumas escolas não concede acesso à rede
    /// inteira nem altera usuários que atuam fora das suas escolas.
    /// </summary>
    private async Task<UserCommandResult?> ValidateEscolasAsync(Perfil perfil, IReadOnlyList<int> escolaIds, IReadOnlyList<UsuarioPerfil>? atuais)
    {
        foreach (var escolaId in escolaIds)
        {
            if (await _escolaRepository.GetByIdAsync(escolaId) is null)
                return Invalid("Escola de atuação não encontrada.");
        }

        var escopo = _tenantContext.EscolaIds;
        if (escopo is null)
            return null;

        if (atuais is { Count: > 0 })
        {
            var atuaisForaDoEscopo = atuais.Any(up => up.EscolaId is null
                ? !IsProfessor(up.Perfil)
                : !escopo.Contains(up.EscolaId.Value));
            if (atuaisForaDoEscopo)
                return Invalid("Você só pode alterar o perfil e as escolas de usuários que atuam apenas nas suas escolas.");
        }

        // No perfil Professor, sem escolas significa "as escolas em que leciona", não a rede inteira.
        if (escolaIds.Count == 0 && !IsProfessor(perfil))
            return Invalid("Selecione as escolas de atuação. Somente usuários com acesso a todas as escolas podem conceder esse acesso.");

        return null;
    }

    private static bool MesmoAcesso(User user, int perfilId, IReadOnlyList<int> escolaIds)
    {
        var atuais = user.UsuariosPerfis;
        if (atuais.Count == 0 || atuais.Any(up => up.PerfilId != perfilId))
            return false;

        var escolasAtuais = atuais.Where(up => up.EscolaId is not null).Select(up => up.EscolaId!.Value).ToHashSet();
        return escolasAtuais.SetEquals(escolaIds);
    }

    private async Task<UserCommandResult> ValidateForCreateAsync(UserRequest request)
    {
        var baseValidation = ValidateCommonFields(request);
        if (!baseValidation.Success) return baseValidation;

        if (!string.IsNullOrWhiteSpace(request.Senha) && !PasswordPolicy.IsMatch(request.Senha))
            return Invalid("Senha fora da politica de seguranca (minimo 8 caracteres, maiuscula, minuscula, numero e caractere especial).");

        var existingByEmail = await _userRepository.GetByEmailOrCpfAsync(request.Email);
        if (existingByEmail is not null)
            return new UserCommandResult(false, "Ja existe um usuario com este e-mail.", Error: UserResultError.Conflict);

        var existingByCpf = await _userRepository.GetByCpfAsync(request.Cpf);
        if (existingByCpf is not null)
            return new UserCommandResult(false, "Ja existe um usuario com este CPF.", Error: UserResultError.Conflict);

        return new UserCommandResult(true, string.Empty);
    }

    private async Task<UserCommandResult> ValidateForUpdateAsync(UserRequest request, int excludeId)
    {
        var baseValidation = ValidateCommonFields(request);
        if (!baseValidation.Success) return baseValidation;

        if (!string.IsNullOrWhiteSpace(request.Senha) && !PasswordPolicy.IsMatch(request.Senha))
            return Invalid("Senha fora da politica de seguranca (minimo 8 caracteres, maiuscula, minuscula, numero e caractere especial).");

        var existingByEmail = await _userRepository.GetByEmailOrCpfAsync(request.Email);
        if (existingByEmail is not null && existingByEmail.Id != excludeId)
            return new UserCommandResult(false, "Ja existe um usuario com este e-mail.", Error: UserResultError.Conflict);

        var existingByCpf = await _userRepository.GetByCpfAsync(request.Cpf);
        if (existingByCpf is not null && existingByCpf.Id != excludeId)
            return new UserCommandResult(false, "Ja existe um usuario com este CPF.", Error: UserResultError.Conflict);

        return new UserCommandResult(true, string.Empty);
    }

    private static UserCommandResult ValidateCommonFields(UserRequest request)
    {
        if (!IsValidEmail(request.Email))
            return Invalid("E-mail invalido.");

        if (!IsValidCpf(request.Cpf))
            return Invalid("CPF invalido (informe 11 digitos validos).");

        if (request.Status != User.StatusAtivo && request.Status != User.StatusInativo && request.Status != User.StatusBloqueado)
            return Invalid("Status invalido. Valores permitidos: ATIVO, INATIVO ou BLOQUEADO.");

        return new UserCommandResult(true, string.Empty);
    }

    private static UserRequest NormalizeRequest(UserRequest request)
    {
        return new UserRequest
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            Cpf = NonDigits.Replace(request.Cpf ?? string.Empty, string.Empty),
            DataNascimento = request.DataNascimento,
            Senha = string.IsNullOrWhiteSpace(request.Senha) ? null : request.Senha.Trim(),
            Status = (request.Status ?? string.Empty).Trim().ToUpperInvariant(),
            PerfilId = request.PerfilId,
            EscolaIds = (request.EscolaIds ?? []).Where(id => id > 0).Distinct().Order().ToList(),
        };
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

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

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var numbers = cpf.Select(c => c - '0').ToArray();
        var firstDigit = CalculateCpfDigit(numbers, 9, 10);
        if (numbers[9] != firstDigit) return false;

        var secondDigit = CalculateCpfDigit(numbers, 10, 11);
        return numbers[10] == secondDigit;
    }

    private static int CalculateCpfDigit(int[] numbers, int length, int weightStart)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += numbers[i] * (weightStart - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static UserCommandResult Invalid(string message)
        => new(false, message, Error: UserResultError.Validation);

    private static bool IsAdministrador(Perfil perfil)
        => string.Equals(perfil.Nome, Perfil.Administrador, StringComparison.OrdinalIgnoreCase);

    private static bool IsProfessor(Perfil? perfil)
        => string.Equals(perfil?.Nome, Perfil.Professor, StringComparison.OrdinalIgnoreCase);

    private static UserResponse MapToResponse(User user)
    {
        // Todas as linhas do usuário têm o mesmo perfil: uma por escola de atuação ou uma sem escola.
        var perfil = user.UsuariosPerfis.FirstOrDefault();
        return new UserResponse(
            user.Id,
            user.Email,
            user.Cpf,
            user.DataNascimento,
            user.Status,
            user.UltimoAcesso,
            user.CreatedAt,
            perfil?.PerfilId,
            perfil?.Perfil?.Nome,
            user.UsuariosPerfis.Where(up => up.EscolaId is not null).Select(up => up.EscolaId!.Value).Order().ToList()
        );
    }
}
