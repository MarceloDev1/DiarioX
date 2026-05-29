using MailKit.Security;
using MimeKit;

namespace DiarioX.Server.Infrastructure.Services;

public interface IEmailSmtpClient : IAsyncDisposable
{
    Task ConnectAsync(string host, int port, SecureSocketOptions options);
    Task AuthenticateAsync(string username, string password);
    Task SendAsync(MimeMessage message);
    Task DisconnectAsync(bool quit);
}
