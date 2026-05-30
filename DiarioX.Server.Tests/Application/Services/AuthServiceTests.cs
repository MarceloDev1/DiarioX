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

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Token));
        Assert.Equal("usuario@x.com", result.Email);
        Assert.True(user.UltimoAcesso.HasValue);
        fixture.UserRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenUserInactive_ReturnsNull()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");
        user.Status = User.StatusInativo;

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "Senha@123" });

        Assert.Null(result);
        fixture.UserRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordInvalid_ReturnsNull()
    {
        var fixture = BuildService();
        var user = BuildActiveUser(email: "usuario@x.com", cpf: "52998224725", password: "Senha@123");

        fixture.UserRepository
            .Setup(r => r.GetByEmailOrCpfAsync("usuario@x.com"))
            .ReturnsAsync(user);

        var result = await fixture.Service.LoginAsync(new LoginRequest { Login = "usuario@x.com", Password = "errada" });

        Assert.Null(result);
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
        BuildService()
    {
        var userRepository = new Mock<IUserRepository>();
        var passwordResetTokenRepository = new Mock<IPasswordResetTokenRepository>();
        var emailService = new Mock<IEmailService>();
        var logger = new Mock<ILogger<AuthService>>();

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
            emailService.Object,
            configuration,
            logger.Object);

        return (service, userRepository, passwordResetTokenRepository, emailService, logger);
    }
}
