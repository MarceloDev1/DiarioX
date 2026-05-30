using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
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

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        const string genericMessage = "Se o cadastro existir, enviaremos um link de redefinição de senha.";

        var rawLogin = request.Login.Trim();
        if (string.IsNullOrWhiteSpace(rawLogin))
            return new ForgotPasswordResponse(true, genericMessage);

        var login = NormalizeLogin(rawLogin);
        if (!IsValidLogin(login))
            return new ForgotPasswordResponse(true, genericMessage);

        var user = await _userRepository.GetByEmailOrCpfAsync(login);
        if (user is null || user.Status != User.StatusAtivo || string.IsNullOrEmpty(user.Email))
            return new ForgotPasswordResponse(true, genericMessage);

        await _passwordResetTokenRepository.InvalidatePreviousTokensAsync(user.Id);

        var (plainToken, tokenHash) = GenerateSecureToken();
        var resetToken = PasswordResetToken.Create(user.Id, tokenHash);
        await _passwordResetTokenRepository.AddAsync(resetToken);

        var appUrl = _configuration["AppUrl"] ?? "https://localhost:5173";
        var resetUrl = $"{appUrl}/redefinir-senha?token={Uri.EscapeDataString(plainToken)}";

        try
        {
            await _emailService.SendAsync(user.Email, null, "Redefinição de senha - Diário de Classe", BuildResetEmailHtml(resetUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de recuperação para {Email}", user.Email);
        }

        return new ForgotPasswordResponse(true, genericMessage);
    }

    public async Task<ForgotPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
            return new ForgotPasswordResponse(false, "Dados inválidos.");

        if (!PasswordPolicy.IsMatch(request.Password))
            return new ForgotPasswordResponse(false, "Senha fora da política de segurança.");

        var tokenHash = ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var resetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash);

        if (resetToken is null || !resetToken.IsValid())
            return new ForgotPasswordResponse(false, "Link de redefinição inválido ou expirado.");

        var user = resetToken.User ?? await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user is null || user.Status != User.StatusAtivo)
            return new ForgotPasswordResponse(false, "Usuário não encontrado ou inativo.");

        user.SetPassword(request.Password);
        await _userRepository.UpdateAsync(user);

        resetToken.UsedAt = DateTime.UtcNow;
        await _passwordResetTokenRepository.UpdateAsync(resetToken);

        return new ForgotPasswordResponse(true, "Senha redefinida com sucesso.");
    }

    private static (string plainToken, string tokenHash) GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var plainToken = Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var tokenHash = ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));
        return (plainToken, tokenHash);
    }

    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static string BuildResetEmailHtml(string resetUrl) => $"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head><meta charset="UTF-8"><title>Redefinição de Senha</title></head>
        <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:40px">
          <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;padding:40px">
            <h2 style="color:#333">Redefinição de Senha</h2>
            <p>Você solicitou a redefinição de senha no <strong>Diário de Classe</strong>.</p>
            <p>Clique no botão abaixo para criar uma nova senha. O link é válido por 60 minutos.</p>
            <a href="{resetUrl}"
               style="display:inline-block;padding:12px 24px;background:#4f46e5;color:#fff;text-decoration:none;border-radius:6px;font-weight:bold">
              Redefinir Senha
            </a>
            <p style="color:#888;font-size:13px;margin-top:24px">
              Se você não solicitou esta redefinição, ignore este e-mail.
            </p>
          </div>
        </body>
        </html>
        """;

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
