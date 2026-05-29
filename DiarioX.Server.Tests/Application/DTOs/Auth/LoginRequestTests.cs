using System.ComponentModel.DataAnnotations;
using DiarioX.Server.Application.DTOs.Auth;

namespace DiarioX.Server.Tests.Application.DTOs.Auth;

public class LoginRequestTests
{
    [Fact]
    public void GetEffectiveLogin_WhenLoginProvided_PrefersLoginAndTrims()
    {
        var request = new LoginRequest
        {
            Login = "  usuario@x.com ",
            Username = "  52998224725 "
        };

        var result = request.GetEffectiveLogin();

        Assert.Equal("usuario@x.com", result);
    }

    [Fact]
    public void GetEffectiveLogin_WhenLoginMissing_UsesUsernameTrimmed()
    {
        var request = new LoginRequest
        {
            Login = null,
            Username = "  52998224725 "
        };

        var result = request.GetEffectiveLogin();

        Assert.Equal("52998224725", result);
    }

    [Fact]
    public void Validate_WhenLoginMissing_ReturnsLoginRequiredError()
    {
        var request = new LoginRequest
        {
            Login = null,
            Username = " ",
            Password = "Senha@123"
        };

        var errors = request.Validate(new ValidationContext(request)).ToList();

        Assert.Contains(errors, e => e.ErrorMessage == "Login é obrigatório.");
    }

    [Fact]
    public void Validate_WhenLoginInvalid_ReturnsLoginFormatError()
    {
        var request = new LoginRequest
        {
            Login = "login-invalido",
            Password = "Senha@123"
        };

        var errors = request.Validate(new ValidationContext(request)).ToList();

        Assert.Contains(errors, e => e.ErrorMessage == "Login deve ser um e-mail valido ou CPF com 11 digitos.");
    }

    [Fact]
    public void Validate_WhenPasswordMissing_ReturnsPasswordRequiredError()
    {
        var request = new LoginRequest
        {
            Login = "usuario@x.com",
            Password = " "
        };

        var errors = request.Validate(new ValidationContext(request)).ToList();

        Assert.Contains(errors, e => e.ErrorMessage == "Senha é obrigatória.");
    }

    [Fact]
    public void Validate_WhenLoginAndPasswordValid_ReturnsNoErrors()
    {
        var request = new LoginRequest
        {
            Login = "usuario@x.com",
            Password = "Senha@123"
        };

        var errors = request.Validate(new ValidationContext(request)).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenCpfLoginValid_ReturnsNoErrors()
    {
        var request = new LoginRequest
        {
            Login = "529.982.247-25",
            Password = "Senha@123"
        };

        var errors = request.Validate(new ValidationContext(request)).ToList();

        Assert.Empty(errors);
    }
}
