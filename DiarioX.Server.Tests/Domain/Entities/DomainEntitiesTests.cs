using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Tests.Domain.Entities;

public class DomainEntitiesTests
{
    [Fact]
    public void User_SetPasswordAndVerifyPassword_WorksAsExpected()
    {
        var user = new User();

        user.SetPassword("Senha@123");

        Assert.False(string.IsNullOrWhiteSpace(user.SenhaHash));
        Assert.NotEqual("Senha@123", user.SenhaHash);
        Assert.True(user.VerifyPassword("Senha@123"));
        Assert.False(user.VerifyPassword("SenhaErrada@123"));
    }

    [Fact]
    public void PasswordResetToken_Create_SetsUserIdHashAndExpiration()
    {
        var before = DateTime.UtcNow;

        var token = PasswordResetToken.Create(5, "token-hash");

        var after = DateTime.UtcNow;

        Assert.Equal(5, token.UserId);
        Assert.Equal("token-hash", token.TokenHash);
        Assert.True(token.ExpiresAt >= before.AddMinutes(PasswordResetToken.ExpirationMinutes));
        Assert.True(token.ExpiresAt <= after.AddMinutes(PasswordResetToken.ExpirationMinutes).AddSeconds(1));
    }

    [Fact]
    public void PasswordResetToken_IsValid_ReturnsTrueWhenNotUsedAndNotExpired()
    {
        var token = new PasswordResetToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UsedAt = null
        };

        Assert.True(token.IsValid());
    }

    [Fact]
    public void PasswordResetToken_IsValid_ReturnsFalseWhenExpiredOrUsed()
    {
        var expired = new PasswordResetToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            UsedAt = null
        };

        var used = new PasswordResetToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UsedAt = DateTime.UtcNow
        };

        Assert.False(expired.IsValid());
        Assert.False(used.IsValid());
    }

    [Fact]
    public void EmailLog_DefaultValues_AreInitialized()
    {
        var log = new EmailLog();

        Assert.Equal(EmailLog.StatusPendente, log.Status);
        Assert.Equal(0, log.Tentativas);
        Assert.True(log.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void EmailTemplate_DefaultValues_AreInitialized()
    {
        var template = new EmailTemplate();

        Assert.True(template.Ativo);
        Assert.NotNull(template.Variaveis);
        Assert.Empty(template.Variaveis);
        Assert.NotNull(template.EmailLogs);
        Assert.Empty(template.EmailLogs);
        Assert.True(template.CreatedAt <= DateTime.UtcNow);
        Assert.True(template.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Escola_DefaultStatus_IsAtivo()
    {
        var escola = new Escola();

        Assert.Equal(Escola.StatusAtivo, escola.Status);
    }

    [Fact]
    public void UsuarioPerfil_Properties_CanBeAssigned()
    {
        var user = new User { Id = 1, Email = "usuario@x.com", Cpf = "52998224725" };
        var perfil = new Perfil { Id = 2, Nome = "Diretor", Descricao = "Perfil diretor" };
        var escola = new Escola { Id = 3, Nome = "Escola A", CodigoInep = "12345678" };

        var usuarioPerfil = new UsuarioPerfil
        {
            Id = 10,
            UsuarioId = user.Id,
            Usuario = user,
            PerfilId = perfil.Id,
            Perfil = perfil,
            EscolaId = escola.Id,
            Escola = escola
        };

        Assert.Equal(10, usuarioPerfil.Id);
        Assert.Equal(1, usuarioPerfil.UsuarioId);
        Assert.Equal(2, usuarioPerfil.PerfilId);
        Assert.Equal(3, usuarioPerfil.EscolaId);
        Assert.Equal("Diretor", usuarioPerfil.Perfil.Nome);
        Assert.Equal("Escola A", usuarioPerfil.Escola!.Nome);
    }
}
