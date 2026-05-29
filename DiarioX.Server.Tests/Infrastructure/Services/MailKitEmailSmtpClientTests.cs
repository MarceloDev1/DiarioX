using DiarioX.Server.Infrastructure.Services;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DiarioX.Server.Tests.Infrastructure.Services;

public class MailKitEmailSmtpClientTests
{
    [Fact]
    public async Task ConnectAsync_WithEmptyHost_ThrowsArgumentException()
    {
        await using var wrapper = new MailKitEmailSmtpClient(new SmtpClient());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            wrapper.ConnectAsync(string.Empty, 25, SecureSocketOptions.None));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenNotConnected_ThrowsServiceNotConnected()
    {
        await using var wrapper = new MailKitEmailSmtpClient(new SmtpClient());

        await Assert.ThrowsAsync<ServiceNotConnectedException>(() =>
            wrapper.AuthenticateAsync("user", "pass"));
    }

    [Fact]
    public async Task SendAsync_WhenNotConnected_ThrowsServiceNotConnected()
    {
        await using var wrapper = new MailKitEmailSmtpClient(new SmtpClient());
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("from", "from@x.com"));
        message.To.Add(new MailboxAddress("to", "to@x.com"));
        message.Subject = "s";
        message.Body = new TextPart("plain") { Text = "b" };

        await Assert.ThrowsAsync<ServiceNotConnectedException>(() =>
            wrapper.SendAsync(message));
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_CompletesSilently()
    {
        await using var wrapper = new MailKitEmailSmtpClient(new SmtpClient());

        await wrapper.DisconnectAsync(true);
    }

    [Fact]
    public async Task DisposeAsync_DisposesInnerClient()
    {
        var inner = new SmtpClient();
        var wrapper = new MailKitEmailSmtpClient(inner);

        await wrapper.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            inner.ConnectAsync("localhost", 25, SecureSocketOptions.None));
    }
}
