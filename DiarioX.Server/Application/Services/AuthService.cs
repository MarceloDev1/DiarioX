using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Text;
using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Application.Services;

public class AuthService : IAuthService
{
    private static readonly Regex NonDigits = new("\\D", RegexOptions.Compiled);
    private static readonly Regex PasswordPolicy = new("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z\\d]).{8,}$", RegexOptions.Compiled);

    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var rawLogin = request.GetEffectiveLogin();
        if (string.IsNullOrWhiteSpace(rawLogin) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var login = NormalizeLogin(rawLogin);
        if (string.IsNullOrWhiteSpace(login) || !IsValidLogin(login))
            return null;

        var user = await _userRepository.GetByEmailOrCpfAsync(login);

        if (user is null)
            return null;

        if (user.Status != User.StatusAtivo)
            return null;

        if (!user.VerifyPassword(request.Password))
            return null;

        user.UltimoAcesso = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var token = GenerateJwtToken(user.Email);
        return token;
    }

    public async Task<FirstAccessOperationResponse> ValidateFirstAccessAsync(FirstAccessValidationRequest request)
    {
        var normalizedCpf = NonDigits.Replace(request.Cpf ?? string.Empty, string.Empty);
        if (!IsValidCpf(normalizedCpf))
            return new FirstAccessOperationResponse(false, "CPF invalido.");

        if (!request.BirthDate.HasValue)
            return new FirstAccessOperationResponse(false, "Data de nascimento obrigatoria.");

        var user = await _userRepository.GetByCpfAsync(normalizedCpf);
        if (user is null)
            return new FirstAccessOperationResponse(false, "Dados institucionais nao encontrados.");

        if (!user.DataNascimento.HasValue || user.DataNascimento.Value.Date != request.BirthDate.Value.Date)
            return new FirstAccessOperationResponse(false, "Dados institucionais nao encontrados.");

        if (user.Status == User.StatusBloqueado)
            return new FirstAccessOperationResponse(false, "Conta bloqueada. Entre em contato com o suporte.");

        return new FirstAccessOperationResponse(true, "Dados institucionais validados com sucesso.");
    }

    public async Task<FirstAccessOperationResponse> ActivateFirstAccessAsync(FirstAccessActivationRequest request)
    {
        var validationResult = await ValidateFirstAccessAsync(request);
        if (!validationResult.Success)
            return validationResult;

        var normalizedCpf = NonDigits.Replace(request.Cpf ?? string.Empty, string.Empty);
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!IsValidEmail(normalizedEmail))
            return new FirstAccessOperationResponse(false, "E-mail invalido.");

        if (string.IsNullOrWhiteSpace(request.Password) || !PasswordPolicy.IsMatch(request.Password))
            return new FirstAccessOperationResponse(false, "Senha fora da politica de seguranca.");

        var currentUser = await _userRepository.GetByCpfAsync(normalizedCpf);
        if (currentUser is null)
            return new FirstAccessOperationResponse(false, "Usuario nao encontrado para ativacao.");

        var userByEmail = await _userRepository.GetByEmailOrCpfAsync(normalizedEmail);
        if (userByEmail is not null && userByEmail.Cpf != normalizedCpf)
            return new FirstAccessOperationResponse(false, "Este e-mail ja esta em uso.");

        currentUser.Email = normalizedEmail;
        currentUser.SetPassword(request.Password);
        currentUser.Status = User.StatusAtivo;

        await _userRepository.UpdateAsync(currentUser);

        return new FirstAccessOperationResponse(true, "Conta ativada com sucesso.");
    }

    private LoginResponse GenerateJwtToken(string email)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new LoginResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            Email: email,
            ExpiresAt: expiresAt
        );
    }

    private static string NormalizeLogin(string login)
    {
        var trimmed = login.Trim();
        return trimmed.Contains('@')
            ? trimmed.ToLowerInvariant()
            : NonDigits.Replace(trimmed, string.Empty);
    }

    private static bool IsValidLogin(string login)
    {
        if (login.Contains('@'))
            return IsValidEmail(login);

        return IsValidCpf(login);
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

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var numbers = cpf.Select(c => c - '0').ToArray();

        var firstDigit = CalculateCpfDigit(numbers, 9, 10);
        if (numbers[9] != firstDigit)
            return false;

        var secondDigit = CalculateCpfDigit(numbers, 10, 11);
        return numbers[10] == secondDigit;
    }

    private static int CalculateCpfDigit(int[] numbers, int length, int weightStart)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * (weightStart - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
