using DiarioX.Server.Infrastructure.Services;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
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

    [Fact]
    public async Task SendAsync_WhenValidConfig_SendsEmailSuccessfully()
    {
        var fakeClient = new FakeEmailSmtpClient();
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.host.local",
            ["Smtp:Port"] = "2525",
            ["Smtp:Username"] = "smtp-user@x.com",
            ["Smtp:Password"] = "smtp-pass",
            ["Smtp:FromEmail"] = "no-reply@x.com",
            ["Smtp:FromName"] = "DiarioX"
        }, fakeClient);

        await service.SendAsync(" destino@x.com ", "Destino", "Assunto teste", "<p>teste</p>");

        Assert.True(fakeClient.ConnectCalled);
        Assert.Equal("smtp.host.local", fakeClient.Host);
        Assert.Equal(2525, fakeClient.Port);
        Assert.Equal(SecureSocketOptions.None, fakeClient.Options);
        Assert.True(fakeClient.AuthenticateCalled);
        Assert.Equal("smtp-user@x.com", fakeClient.Username);
        Assert.Equal("smtp-pass", fakeClient.Password);
        Assert.True(fakeClient.SendCalled);
        Assert.NotNull(fakeClient.Message);
        Assert.Equal("Assunto teste", fakeClient.Message!.Subject);
        Assert.Equal("no-reply@x.com", ((MailboxAddress)fakeClient.Message.From[0]).Address);
        Assert.Equal("destino@x.com", ((MailboxAddress)fakeClient.Message.To[0]).Address);
        Assert.True(fakeClient.DisconnectCalled);
    }

    [Fact]
    public async Task SendAsync_WhenSmtpFails_RethrowsException()
    {
        var fakeClient = new FakeEmailSmtpClient { ThrowOnSend = true };
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp.host.local",
            ["Smtp:Username"] = "smtp-user@x.com",
            ["Smtp:Password"] = "smtp-pass"
        }, fakeClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync("destino@x.com", null, "Assunto", "<p>teste</p>"));

        Assert.True(fakeClient.ConnectCalled);
        Assert.True(fakeClient.AuthenticateCalled);
        Assert.True(fakeClient.SendCalled);
        Assert.False(fakeClient.DisconnectCalled);
    }

    private static EmailService BuildService(Dictionary<string, string?> settings, FakeEmailSmtpClient? fakeClient = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var logger = new Mock<ILogger<EmailService>>();
        if (fakeClient is null)
            return new EmailService(configuration, logger.Object);

        return new EmailService(configuration, logger.Object, () => fakeClient);
    }

    private sealed class FakeEmailSmtpClient : IEmailSmtpClient
    {
        public bool ThrowOnSend { get; set; }
        public bool ConnectCalled { get; private set; }
        public bool AuthenticateCalled { get; private set; }
        public bool SendCalled { get; private set; }
        public bool DisconnectCalled { get; private set; }
        public string? Host { get; private set; }
        public int Port { get; private set; }
        public SecureSocketOptions Options { get; private set; }
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public MimeMessage? Message { get; private set; }

        public Task ConnectAsync(string host, int port, SecureSocketOptions options)
        {
            ConnectCalled = true;
            Host = host;
            Port = port;
            Options = options;
            return Task.CompletedTask;
        }

        public Task AuthenticateAsync(string username, string password)
        {
            AuthenticateCalled = true;
            Username = username;
            Password = password;
            return Task.CompletedTask;
        }

        public Task SendAsync(MimeMessage message)
        {
            SendCalled = true;
            Message = message;

            if (ThrowOnSend)
                throw new InvalidOperationException("smtp send failure");

            return Task.CompletedTask;
        }

        public Task DisconnectAsync(bool quit)
        {
            DisconnectCalled = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
