using System.Net.Mail;
using System.Text.RegularExpressions;
using DiarioX.Server.Application.DTOs.Users;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;

namespace DiarioX.Server.Application.Services;

public class UserService : IUserService
{
    private static readonly Regex NonDigits = new("\\D", RegexOptions.Compiled);
    private static readonly Regex PasswordPolicy = new(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z\\d]).{8,}$",
        RegexOptions.Compiled);

    private readonly IUserRepository _userRepository;
    private readonly IPerfilRepository _perfilRepository;
    private readonly IUsuarioPerfilRepository _usuarioPerfilRepository;

    public UserService(
        IUserRepository userRepository,
        IPerfilRepository perfilRepository,
        IUsuarioPerfilRepository usuarioPerfilRepository)
    {
        _userRepository = userRepository;
        _perfilRepository = perfilRepository;
        _usuarioPerfilRepository = usuarioPerfilRepository;
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

        if (normalized.PerfilId.HasValue)
        {
            var perfil = await _perfilRepository.GetByIdAsync(normalized.PerfilId.Value);
            if (perfil is null)
                return Invalid("Perfil nao encontrado.");
        }

        var validation = await ValidateForCreateAsync(normalized);
        if (!validation.Success)
            return validation;

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

        if (normalized.PerfilId.HasValue)
        {
            await _usuarioPerfilRepository.AddAsync(new UsuarioPerfil
            {
                UsuarioId = created.Id,
                PerfilId = normalized.PerfilId.Value,
                EscolaId = null,
            });
        }

        var result = await _userRepository.GetByIdAsync(created.Id);
        return new UserCommandResult(true, "Usuario cadastrado com sucesso.", MapToResponse(result!));
    }

    public async Task<UserCommandResult> UpdateAsync(int id, UserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return new UserCommandResult(false, "Usuario nao encontrado.", Error: UserResultError.NotFound);

        var normalized = NormalizeRequest(request);

        if (normalized.PerfilId.HasValue)
        {
            var perfil = await _perfilRepository.GetByIdAsync(normalized.PerfilId.Value);
            if (perfil is null)
                return Invalid("Perfil nao encontrado.");
        }

        var validation = await ValidateForUpdateAsync(normalized, id);
        if (!validation.Success)
            return validation;

        user.Email = normalized.Email;
        user.Cpf = normalized.Cpf;
        user.DataNascimento = normalized.DataNascimento;
        user.Status = normalized.Status;

        if (!string.IsNullOrWhiteSpace(normalized.Senha))
            user.SetPassword(normalized.Senha);

        await _userRepository.UpdateAsync(user);

        if (normalized.PerfilId.HasValue)
        {
            var existing = await _usuarioPerfilRepository.GetGlobalByUsuarioIdAsync(id);
            if (existing is null)
            {
                await _usuarioPerfilRepository.AddAsync(new UsuarioPerfil
                {
                    UsuarioId = id,
                    PerfilId = normalized.PerfilId.Value,
                    EscolaId = null,
                });
            }
            else if (existing.PerfilId != normalized.PerfilId.Value)
            {
                existing.PerfilId = normalized.PerfilId.Value;
                await _usuarioPerfilRepository.UpdateAsync(existing);
            }
        }

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
                "Nao e possivel remover este usuario pois ele esta associado a outros registros.",
                Error: UserResultError.Conflict);
        }

        return new UserCommandResult(true, "Usuario removido com sucesso.");
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

    private static UserResponse MapToResponse(User user)
    {
        var globalPerfil = user.UsuariosPerfis.FirstOrDefault(up => up.EscolaId == null);
        return new UserResponse(
            user.Id,
            user.Email,
            user.Cpf,
            user.DataNascimento,
            user.Status,
            user.UltimoAcesso,
            user.CreatedAt,
            globalPerfil?.PerfilId,
            globalPerfil?.Perfil?.Nome
        );
    }
}
