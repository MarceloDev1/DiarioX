using System.IdentityModel.Tokens.Jwt;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsTokenAndUpdatesLastAccess()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var request = new LoginRequest { Login = "  Usuario@X.com ", Password = "Senha@123" };

        var result = await fixture.Service.LoginAsync(request);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Response!.Token));
        Assert.Equal("usuario@x.com", result.Response.Email);
        Assert.True(user.UltimoAcesso.HasValue);
        fixture.UserRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenUserInactive_ReturnsUserInactiveFailure()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.Status = User.StatusInativo;

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "Senha@123" });

        Assert.False(result.Success);
        Assert.Equal(LoginFailureReason.UserInactive, result.FailureReason);
        fixture.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserBlocked_ReturnsUserBlockedFailure()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.Status = User.StatusBloqueado;

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "Senha@123" });

        Assert.False(result.Success);
        Assert.Equal(LoginFailureReason.UserBlocked, result.FailureReason);
        fixture.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordInvalid_ReturnsInvalidCredentialsAndRecordsAttempt()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "errada" });

        Assert.False(result.Success);
        Assert.Equal(LoginFailureReason.InvalidCredentials, result.FailureReason);
        Assert.Equal(1, user.FailedLoginAttempts);
        fixture.UserRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenFifthConsecutiveFailure_LocksAccountTemporarily()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.FailedLoginAttempts = User.MaxFailedLoginAttempts - 1;

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "errada" });

        Assert.False(result.Success);
        Assert.Equal(LoginFailureReason.AccountLocked, result.FailureReason);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.True(user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountCurrentlyLockedOut_ReturnsAccountLockedWithoutCheckingPassword()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "Senha@123" });

        Assert.False(result.Success);
        Assert.Equal(LoginFailureReason.AccountLocked, result.FailureReason);
        fixture.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ValidateFirstAccessAsync_WhenCpfInvalid_ReturnsFailure()
    {
        var fixture = BuildService();

        var result = await fixture.Service.ValidateFirstAccessAsync(new FirstAccessValidationRequest
        {
            Cpf = "123",
            BirthDate = new DateTime(1990, 1, 1)
        });

        Assert.False(result.Success);
        Assert.Equal("CPF invalido.", result.Message);
    }

    [Fact]
    public async Task ValidateFirstAccessAsync_WhenBirthDateMissing_ReturnsFailure()
    {
        var fixture = BuildService();

        var result = await fixture.Service.ValidateFirstAccessAsync(new FirstAccessValidationRequest
        {
            Cpf = "52998224725",
            BirthDate = null
        });

        Assert.False(result.Success);
        Assert.Equal("Data de nascimento obrigatoria.", result.Message);
    }

    [Fact]
    public async Task ValidateFirstAccessAsync_WhenDataMatches_ReturnsSuccess()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123", birthDate: new DateTime(1990, 1, 1));

        fixture.UserRepository
            .Setup(r => r.GetByCpfAsync("52998224725"))
            .ReturnsAsync(user);

        var result = await fixture.Service.ValidateFirstAccessAsync(new FirstAccessValidationRequest
        {
            Cpf = "529.982.247-25",
            BirthDate = new DateTime(1990, 1, 1)
        });

        Assert.True(result.Success);
        Assert.Equal("Dados institucionais validados com sucesso.", result.Message);
    }

    [Fact]
    public async Task ActivateFirstAccessAsync_WhenEmailAlreadyUsedByAnotherUser_ReturnsFailure()
    {
        var fixture = BuildService();
        var currentUser = BuildActiveUser(email: "old@x.com", cpf: "52998224725", password: "Senha@123", birthDate: new DateTime(1990, 1, 1));
        var otherUser = BuildActiveUser(email: "email@x.com", cpf: "11144477735", password: "Senha@123", birthDate: new DateTime(1988, 1, 1));

        fixture.UserRepository.Setup(r => r.GetByCpfAsync("52998224725")).ReturnsAsync(currentUser);
        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("email@x.com")).ReturnsAsync(otherUser);

        var request = new FirstAccessActivationRequest
        {
            Cpf = "52998224725",
            BirthDate = new DateTime(1990, 1, 1),
            Email = "email@x.com",
            Password = "Senha@123"
        };

        var result = await fixture.Service.ActivateFirstAccessAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Este e-mail ja esta em uso.", result.Message);
        fixture.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ActivateFirstAccessAsync_WhenValid_ActivatesUser()
    {
        var fixture = BuildService();
        var currentUser = BuildActiveUser(email: "old@x.com", cpf: "52998224725", password: "Senha@123", birthDate: new DateTime(1990, 1, 1));
        currentUser.Status = User.StatusInativo;

        fixture.UserRepository.Setup(r => r.GetByCpfAsync("52998224725")).ReturnsAsync(currentUser);
        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("novo@x.com")).ReturnsAsync((User?)null);

        var request = new FirstAccessActivationRequest
        {
            Cpf = "52998224725",
            BirthDate = new DateTime(1990, 1, 1),
            Email = "  Novo@X.com ",
            Password = "Senha@123"
        };

        var result = await fixture.Service.ActivateFirstAccessAsync(request);

        Assert.True(result.Success);
        Assert.Equal("Conta ativada com sucesso.", result.Message);
        Assert.Equal("novo@x.com", currentUser.Email);
        Assert.Equal(User.StatusAtivo, currentUser.Status);
        Assert.True(currentUser.VerifyPassword("Senha@123"));
        fixture.UserRepository.Verify(r => r.UpdateAsync(currentUser), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserNotFound_ReturnsGenericMessageWithoutSendingEmail()
    {
        var fixture = BuildService();
        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com")).ReturnsAsync((User?)null);

        var result = await fixture.Service.ForgotPasswordAsync(new ForgotPasswordRequest("usuario@x.com"));

        Assert.True(result.Success);
        Assert.Equal("Se o cadastro existir, enviaremos um link de redefinição de senha.", result.Message);
        fixture.PasswordResetTokenRepository.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>()), Times.Never);
        fixture.EmailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenValidUser_GeneratesTokenAndSendsEmail()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.Id = 12;

        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com")).ReturnsAsync(user);

        var result = await fixture.Service.ForgotPasswordAsync(new ForgotPasswordRequest("usuario@x.com"));

        Assert.True(result.Success);
        fixture.PasswordResetTokenRepository.Verify(r => r.InvalidatePreviousTokensAsync(12), Times.Once);
        fixture.PasswordResetTokenRepository.Verify(r => r.AddAsync(It.Is<PasswordResetToken>(t => t.UserId == 12 && !string.IsNullOrWhiteSpace(t.TokenHash))), Times.Once);
        fixture.EmailService.Verify(e => e.SendAsync(
            "usuario@x.com",
            null,
            It.Is<string>(s => s.Contains("Redefinição de senha")),
            It.Is<string>(b => b.Contains("token="))), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenPasswordPolicyInvalid_ReturnsFailure()
    {
        var fixture = BuildService();

        var result = await fixture.Service.ResetPasswordAsync(new ResetPasswordRequest("token", "fraca"));

        Assert.False(result.Success);
        Assert.Equal("Senha fora da política de segurança.", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenInvalid_ReturnsFailure()
    {
        var fixture = BuildService();

        fixture.PasswordResetTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync((PasswordResetToken?)null);

        var result = await fixture.Service.ResetPasswordAsync(new ResetPasswordRequest("token-valido", "Senha@123"));

        Assert.False(result.Success);
        Assert.Equal("Link de redefinição inválido ou expirado.", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenValid_UpdatesPasswordAndConsumesToken()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.Id = 40;

        var resetToken = PasswordResetToken.Create(user.Id, "hash");

        fixture.PasswordResetTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(resetToken);

        fixture.UserRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(user);

        var result = await fixture.Service.ResetPasswordAsync(new ResetPasswordRequest("token-plain", "NovaSenha@123"));

        Assert.True(result.Success);
        Assert.Equal("Senha redefinida com sucesso.", result.Message);
        Assert.True(user.VerifyPassword("NovaSenha@123"));
        Assert.True(resetToken.UsedAt.HasValue);

        fixture.UserRepository.Verify(r => r.UpdateAsync(user), Times.Once);
        fixture.PasswordResetTokenRepository.Verify(r => r.UpdateAsync(resetToken), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserLockedOut_ClearsLockoutAndAllowsLogin()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.Id = 41;
        user.FailedLoginAttempts = 3;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);

        var resetToken = PasswordResetToken.Create(user.Id, "hash");

        fixture.PasswordResetTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(resetToken);

        fixture.UserRepository.Setup(r => r.GetByIdAsync(41)).ReturnsAsync(user);
        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com")).ReturnsAsync(user);

        var resetResult = await fixture.Service.ResetPasswordAsync(new ResetPasswordRequest("token-plain", "NovaSenha@123"));

        Assert.True(resetResult.Success);
        Assert.Null(user.LockoutEnd);
        Assert.Equal(0, user.FailedLoginAttempts);

        var loginResult = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "NovaSenha@123" });

        Assert.True(loginResult.Success);
    }

    [Fact]
    public async Task LoginAsync_WhenTenantUser_EmitsTenantClaimsAndTenantData()
    {
        var (fixture, tenantRepository) = BuildServiceWithTenants();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.Id = 42;
        user.TenantId = 7;

        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com")).ReturnsAsync(user);
        tenantRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildTenant(7, "colegio-x"));

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "Senha@123" });

        Assert.True(result.Success);
        Assert.Equal(7, result.Response!.TenantId);
        Assert.Equal("Colégio colegio-x", result.Response.TenantNome);
        Assert.False(result.Response.IsGlobalAdmin);

        var claims = ReadClaims(result.Response.Token);
        Assert.Equal("42", claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("7", claims[AppClaimTypes.TenantId]);
        Assert.Equal("colegio-x", claims[AppClaimTypes.TenantSlug]);
        Assert.False(claims.ContainsKey(AppClaimTypes.GlobalAdmin));
    }

    [Fact]
    public async Task LoginAsync_WhenGlobalAdmin_EmitsGlobalAdminClaimWithoutTenant()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "admin@x.com", cpf: "52998224725", password: "Senha@123");
        user.TenantId = null;

        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("admin@x.com")).ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "admin@x.com", Password = "Senha@123" });

        Assert.True(result.Success);
        Assert.True(result.Response!.IsGlobalAdmin);
        Assert.Null(result.Response.TenantId);

        var claims = ReadClaims(result.Response.Token);
        Assert.Equal("true", claims[AppClaimTypes.GlobalAdmin]);
        Assert.False(claims.ContainsKey(AppClaimTypes.TenantId));
    }

    [Fact]
    public async Task LoginAsync_WhenTenantInactive_ReturnsInvalidCredentials()
    {
        var (fixture, tenantRepository) = BuildServiceWithTenants();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.TenantId = 7;

        var tenant = BuildTenant(7, "colegio-x");
        tenant.Status = Tenant.StatusInativo;

        fixture.UserRepository.Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com")).ReturnsAsync(user);
        tenantRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(tenant);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "Senha@123" });

        Assert.False(result.Success);
        Assert.Equal(LoginFailureReason.InvalidCredentials, result.FailureReason);
        fixture.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SelectTenantAsync_WhenGlobalAdminAndTenantActive_ReturnsTokenForTenant()
    {
        var (fixture, tenantRepository) = BuildServiceWithTenants();
        var admin = BuildActiveUser(email: "admin@x.com", cpf: "52998224725", password: "Senha@123");
        admin.Id = 1;

        fixture.UserRepository.Setup(r => r.GetGlobalByIdAsync(1)).ReturnsAsync(admin);
        tenantRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(BuildTenant(3, "rede-y"));

        var response = await fixture.Service.SelectTenantAsync(1, 3);

        Assert.NotNull(response);
        Assert.Equal(3, response!.TenantId);
        Assert.True(response.IsGlobalAdmin);

        var claims = ReadClaims(response.Token);
        Assert.Equal("3", claims[AppClaimTypes.TenantId]);
        Assert.Equal("rede-y", claims[AppClaimTypes.TenantSlug]);
        Assert.Equal("true", claims[AppClaimTypes.GlobalAdmin]);
    }

    [Fact]
    public async Task SelectTenantAsync_WhenUserIsNotGlobal_ReturnsNull()
    {
        var (fixture, tenantRepository) = BuildServiceWithTenants();

        fixture.UserRepository.Setup(r => r.GetGlobalByIdAsync(5)).ReturnsAsync((User?)null);
        tenantRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(BuildTenant(3, "rede-y"));

        var response = await fixture.Service.SelectTenantAsync(5, 3);

        Assert.Null(response);
    }

    [Fact]
    public async Task SelectTenantAsync_WhenTenantInactive_ReturnsNull()
    {
        var (fixture, tenantRepository) = BuildServiceWithTenants();
        var admin = BuildActiveUser(email: "admin@x.com", cpf: "52998224725", password: "Senha@123");

        var tenant = BuildTenant(3, "rede-y");
        tenant.Status = Tenant.StatusInativo;

        fixture.UserRepository.Setup(r => r.GetGlobalByIdAsync(1)).ReturnsAsync(admin);
        tenantRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(tenant);

        var response = await fixture.Service.SelectTenantAsync(1, 3);

        Assert.Null(response);
    }

    private static Tenant BuildTenant(int id, string slug) => new()
    {
        Id = id,
        Nome = $"Colégio {slug}",
        Slug = slug,
        Status = Tenant.StatusAtivo,
    };

    private static Dictionary<string, string> ReadClaims(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.First().Value);

    private static User BuildActiveUser(string email, string cpf, string password, DateTime? birthDate = null)
    {
        var user = new User
        {
            Email = email,
            Cpf = cpf,
            Status = User.StatusAtivo,
            DataNascimento = birthDate
        };

        user.SetPassword(password);
        return user;
    }

    private static (AuthService Service,
        Mock<IUserRepository> UserRepository,
        Mock<IPasswordResetTokenRepository> PasswordResetTokenRepository,
        Mock<IEmailService> EmailService,
        Mock<ILogger<AuthService>> Logger)
        BuildService() => BuildServiceWithTenants().Fixture;

    private static ((AuthService Service,
        Mock<IUserRepository> UserRepository,
        Mock<IPasswordResetTokenRepository> PasswordResetTokenRepository,
        Mock<IEmailService> EmailService,
        Mock<ILogger<AuthService>> Logger) Fixture,
        Mock<ITenantRepository> TenantRepository)
        BuildServiceWithTenants()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordResetTokenRepository = new Mock<IPasswordResetTokenRepository>();
        var tenantRepository = new Mock<ITenantRepository>();
        var emailService = new Mock<IEmailService>();
        var appUrlProvider = new Mock<IAppUrlProvider>();
        var logger = new Mock<ILogger<AuthService>>();

        appUrlProvider.Setup(p => p.GetAppUrl()).Returns("https://localhost:5173");

        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "super-secret-key-for-tests-only-minimum-size",
            ["Jwt:Issuer"] = "DiarioX.Tests",
            ["Jwt:Audience"] = "DiarioX.Tests.Client",
            ["Jwt:ExpiresInMinutes"] = "60",
            ["AppUrl"] = "https://localhost:5173"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var service = new AuthService(
            userRepository.Object,
            passwordResetTokenRepository.Object,
            tenantRepository.Object,
            emailService.Object,
            appUrlProvider.Object,
            configuration,
            logger.Object);

        return ((service, userRepository, passwordResetTokenRepository, emailService, logger), tenantRepository);
    }
}
