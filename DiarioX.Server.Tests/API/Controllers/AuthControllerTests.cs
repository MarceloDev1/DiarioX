using DiarioX.Server.API.Controllers;
using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorized()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new LoginRequest { Login = "usuario@x.com", Password = "errada" };
        authService.Setup(s => s.LoginAsync(request)).ReturnsAsync((LoginResponse?)null);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.Login(request);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Usuário ou senha inválidos.", GetPropertyValue(unauthorized.Value, "message"));
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new LoginRequest { Login = "usuario@x.com", Password = "Senha@123" };
        var response = new LoginResponse("jwt-token", "usuario@x.com", DateTime.UtcNow.AddMinutes(60));
        authService.Setup(s => s.LoginAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.Login(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task ValidateFirstAccess_WhenValidationFails_ReturnsUnauthorized()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new FirstAccessValidationRequest { Cpf = "123", BirthDate = new DateTime(1990, 1, 1) };
        var response = new FirstAccessOperationResponse(false, "CPF invalido.");

        authService.Setup(s => s.ValidateFirstAccessAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.ValidateFirstAccess(request);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("CPF invalido.", GetPropertyValue(unauthorized.Value, "message"));
    }

    [Fact]
    public async Task ValidateFirstAccess_WhenValidationSucceeds_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new FirstAccessValidationRequest { Cpf = "52998224725", BirthDate = new DateTime(1990, 1, 1) };
        var response = new FirstAccessOperationResponse(true, "Dados institucionais validados com sucesso.");

        authService.Setup(s => s.ValidateFirstAccessAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.ValidateFirstAccess(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task ActivateFirstAccess_WhenActivationFails_ReturnsBadRequest()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new FirstAccessActivationRequest
        {
            Cpf = "52998224725",
            BirthDate = new DateTime(1990, 1, 1),
            Email = "jauso@x.com",
            Password = "Senha@123"
        };
        var response = new FirstAccessOperationResponse(false, "Este e-mail ja esta em uso.");

        authService.Setup(s => s.ActivateFirstAccessAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.ActivateFirstAccess(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Este e-mail ja esta em uso.", GetPropertyValue(badRequest.Value, "message"));
    }

    [Fact]
    public async Task ActivateFirstAccess_WhenActivationSucceeds_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new FirstAccessActivationRequest
        {
            Cpf = "52998224725",
            BirthDate = new DateTime(1990, 1, 1),
            Email = "novo@x.com",
            Password = "Senha@123"
        };
        var response = new FirstAccessOperationResponse(true, "Conta ativada com sucesso.");

        authService.Setup(s => s.ActivateFirstAccessAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.ActivateFirstAccess(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task ForgotPassword_AlwaysReturnsOkWithResponse()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new ForgotPasswordRequest("usuario@x.com");
        var response = new ForgotPasswordResponse(true, "Se o cadastro existir, enviaremos um link de redefinição de senha.");

        authService.Setup(s => s.ForgotPasswordAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.ForgotPassword(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task ResetPassword_WhenResetFails_ReturnsBadRequest()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new ResetPasswordRequest("token", "fraca");
        var response = new ForgotPasswordResponse(false, "Senha fora da política de segurança.");

        authService.Setup(s => s.ResetPasswordAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.ResetPassword(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Senha fora da política de segurança.", GetPropertyValue(badRequest.Value, "message"));
    }

    [Fact]
    public async Task ResetPassword_WhenResetSucceeds_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var request = new ResetPasswordRequest("token", "Senha@123");
        var response = new ForgotPasswordResponse(true, "Senha redefinida com sucesso.");

        authService.Setup(s => s.ResetPasswordAsync(request)).ReturnsAsync(response);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.ResetPassword(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task TestEmail_WhenNotDevelopment_ReturnsNotFound()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: false);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.TestEmail("teste@x.com");

        Assert.IsType<NotFoundResult>(result);
        emailService.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task TestEmail_WhenDevelopmentAndSendSucceeds_ReturnsOk()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: true);

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.TestEmail("teste@x.com");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("E-mail enviado para teste@x.com", GetPropertyValue(ok.Value, "message"));
        emailService.Verify(
            s => s.SendAsync(
                "teste@x.com",
                null,
                "Teste SMTP - Diário de Classe",
                "<p>Configuração SMTP funcionando corretamente.</p>"),
            Times.Once);
    }

    [Fact]
    public async Task TestEmail_WhenDevelopmentAndSendFails_ReturnsBadRequestWithDetail()
    {
        var authService = new Mock<IAuthService>();
        var emailService = new Mock<IEmailService>();
        var env = CreateEnvironment(isDevelopment: true);

        emailService
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp failure"));

        var controller = new AuthController(authService.Object, emailService.Object, env.Object);

        var result = await controller.TestEmail("teste@x.com");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("smtp failure", GetPropertyValue(badRequest.Value, "message"));
        Assert.Contains("InvalidOperationException", GetPropertyValue(badRequest.Value, "detail") ?? string.Empty);
    }

    private static Mock<IWebHostEnvironment> CreateEnvironment(bool isDevelopment)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName)
            .Returns(isDevelopment ? "Development" : "Production");
        return env;
    }

    private static string? GetPropertyValue(object? value, string propertyName)
    {
        if (value is null)
            return null;

        var property = value.GetType().GetProperty(propertyName);
        return property?.GetValue(value)?.ToString();
    }
}
