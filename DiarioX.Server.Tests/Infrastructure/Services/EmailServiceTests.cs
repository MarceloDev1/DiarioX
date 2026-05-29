using DiarioX.Server.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DiarioX.Server.Tests.Infrastructure.Services;

public class EmailServiceTests
{
    [Fact]
    public async Task SendAsync_WhenHostMissing_ThrowsInvalidOperationException()
    {
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:Username"] = "smtp-user",
            ["Smtp:Password"] = "smtp-pass"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync("destino@x.com", null, "Assunto", "<p>teste</p>"));

        Assert.Contains("Smtp:Host", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenUsernameMissing_ThrowsInvalidOperationException()
    {
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.host.local",
            ["Smtp:Password"] = "smtp-pass"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync("destino@x.com", null, "Assunto", "<p>teste</p>"));

        Assert.Contains("Smtp:Username", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenPasswordMissing_ThrowsInvalidOperationException()
    {
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.host.local",
            ["Smtp:Username"] = "smtp-user"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync("destino@x.com", null, "Assunto", "<p>teste</p>"));

        Assert.Contains("Smtp:Password", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenToEmailEmpty_ThrowsArgumentException()
    {
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.host.local",
            ["Smtp:Username"] = "smtp-user",
            ["Smtp:Password"] = "smtp-pass"
        });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendAsync(" ", null, "Assunto", "<p>teste</p>"));

        Assert.Equal("toEmail", ex.ParamName);
    }

    private static EmailService BuildService(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var logger = new Mock<ILogger<EmailService>>();
        return new EmailService(configuration, logger.Object);
    }
}
