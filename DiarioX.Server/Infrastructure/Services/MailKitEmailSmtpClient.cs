using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DiarioX.Server.Infrastructure.Services;

public sealed class MailKitEmailSmtpClient(SmtpClient client) : IEmailSmtpClient
{
    private readonly SmtpClient _client = client;

    public Task ConnectAsync(string host, int port, SecureSocketOptions options)
        => _client.ConnectAsync(host, port, options);

    public Task AuthenticateAsync(string username, string password)
        => _client.AuthenticateAsync(username, password);

    public Task SendAsync(MimeMessage message)
        => _client.SendAsync(message);

    public Task DisconnectAsync(bool quit)
        => _client.DisconnectAsync(quit);

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}
