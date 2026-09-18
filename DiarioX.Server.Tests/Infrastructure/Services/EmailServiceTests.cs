using System.Net;
using DiarioX.Server.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DiarioX.Server.Tests.Infrastructure.Services;

public class EmailServiceTests
{
    [Fact]
    public async Task SendAsync_WhenApiKeyMissing_ThrowsInvalidOperationException()
    {
        var service = BuildService(new Dictionary<string, string?>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync("destino@x.com", null, "Assunto", "<p>teste</p>"));

        Assert.Contains("Smtp:ApiKey", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenToEmailEmpty_ThrowsArgumentException()
    {
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:ApiKey"] = "xkeysib-test-key"
        });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendAsync(" ", null, "Assunto", "<p>teste</p>"));

        Assert.Equal("toEmail", ex.ParamName);
    }

    [Fact]
    public async Task SendAsync_WhenValidConfig_SendsEmailSuccessfully()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Created, """{"messageId":"abc123"}""");
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:ApiKey"] = "xkeysib-test-key",
            ["Smtp:FromEmail"] = "no-reply@x.com",
            ["Smtp:FromName"] = "DiarioX"
        }, handler);

        await service.SendAsync("destino@x.com", "Destino", "Assunto teste", "<p>teste</p>");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("xkeysib-test-key", handler.LastRequest.Headers.GetValues("api-key").First());
    }

    [Fact]
    public async Task SendAsync_WhenApiFails_ThrowsInvalidOperationException()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Unauthorized, """{"message":"unauthorized"}""");
        var service = BuildService(new Dictionary<string, string?>
        {
            ["Smtp:ApiKey"] = "bad-key",
        }, handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync("destino@x.com", null, "Assunto", "<p>teste</p>"));
    }

    private static EmailService BuildService(Dictionary<string, string?> settings, FakeHttpHandler? handler = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var logger = new Mock<ILogger<EmailService>>();
        var httpClient = new HttpClient(handler ?? new FakeHttpHandler(HttpStatusCode.Created, "{}"));

        return new EmailService(configuration, logger.Object, httpClient);
    }

    private sealed class FakeHttpHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
