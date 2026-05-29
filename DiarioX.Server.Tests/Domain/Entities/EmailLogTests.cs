using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Tests.Domain.Entities;

public class EmailLogTests
{
    [Fact]
    public void StatusConstants_HaveExpectedValues()
    {
        Assert.Equal("PENDENTE", EmailLog.StatusPendente);
        Assert.Equal("ENVIADO", EmailLog.StatusEnviado);
        Assert.Equal("FALHOU", EmailLog.StatusFalhou);
    }

    [Fact]
    public void Defaults_AreInitialized()
    {
        var before = DateTime.UtcNow;

        var log = new EmailLog();

        Assert.Equal(0, log.Id);
        Assert.Null(log.TemplateId);
        Assert.Equal(string.Empty, log.DestinatarioEmail);
        Assert.Null(log.DestinatarioNome);
        Assert.Equal(string.Empty, log.Assunto);
        Assert.Equal(string.Empty, log.CorpoHtml);
        Assert.Equal(EmailLog.StatusPendente, log.Status);
        Assert.Equal(0, log.Tentativas);
        Assert.Null(log.ErroMensagem);
        Assert.Null(log.EnviadoEm);
        Assert.True(log.CreatedAt >= before);
        Assert.True(log.CreatedAt <= DateTime.UtcNow);
        Assert.Null(log.Template);
    }

    [Fact]
    public void Properties_CanBeAssigned()
    {
        var template = new EmailTemplate { Id = 7, Nome = "Boas-vindas" };
        var enviadoEm = new DateTime(2026, 5, 29, 10, 30, 0, DateTimeKind.Utc);
        var createdAt = enviadoEm.AddMinutes(-2);

        var log = new EmailLog
        {
            Id = 99,
            TemplateId = template.Id,
            Template = template,
            DestinatarioEmail = "user@x.com",
            DestinatarioNome = "Fulano",
            Assunto = "Bem-vindo",
            CorpoHtml = "<p>Olá</p>",
            Status = EmailLog.StatusEnviado,
            Tentativas = 3,
            ErroMensagem = "timeout",
            EnviadoEm = enviadoEm,
            CreatedAt = createdAt
        };

        Assert.Equal(99, log.Id);
        Assert.Equal(7, log.TemplateId);
        Assert.Same(template, log.Template);
        Assert.Equal("user@x.com", log.DestinatarioEmail);
        Assert.Equal("Fulano", log.DestinatarioNome);
        Assert.Equal("Bem-vindo", log.Assunto);
        Assert.Equal("<p>Olá</p>", log.CorpoHtml);
        Assert.Equal(EmailLog.StatusEnviado, log.Status);
        Assert.Equal(3, log.Tentativas);
        Assert.Equal("timeout", log.ErroMensagem);
        Assert.Equal(enviadoEm, log.EnviadoEm);
        Assert.Equal(createdAt, log.CreatedAt);
    }

    [Theory]
    [InlineData("PENDENTE")]
    [InlineData("ENVIADO")]
    [InlineData("FALHOU")]
    public void Status_AcceptsKnownConstants(string status)
    {
        var log = new EmailLog { Status = status };

        Assert.Equal(status, log.Status);
    }

    [Fact]
    public void Tentativas_CanBeIncremented()
    {
        var log = new EmailLog();

        log.Tentativas++;
        log.Tentativas++;

        Assert.Equal(2, log.Tentativas);
    }

    [Fact]
    public void MarkingAsEnviado_SetsStatusAndTimestamp()
    {
        var log = new EmailLog();
        var when = DateTime.UtcNow;

        log.Status = EmailLog.StatusEnviado;
        log.EnviadoEm = when;

        Assert.Equal(EmailLog.StatusEnviado, log.Status);
        Assert.Equal(when, log.EnviadoEm);
    }

    [Fact]
    public void MarkingAsFalhou_SetsStatusAndErrorMessage()
    {
        var log = new EmailLog();

        log.Status = EmailLog.StatusFalhou;
        log.ErroMensagem = "SMTP host unreachable";

        Assert.Equal(EmailLog.StatusFalhou, log.Status);
        Assert.Equal("SMTP host unreachable", log.ErroMensagem);
        Assert.Null(log.EnviadoEm);
    }
}
