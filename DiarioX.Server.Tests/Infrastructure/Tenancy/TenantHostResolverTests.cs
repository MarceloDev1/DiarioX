using DiarioX.Server.Infrastructure.Tenancy;
using Microsoft.Extensions.Configuration;

namespace DiarioX.Server.Tests.Infrastructure.Tenancy;

public class TenantHostResolverTests
{
    [Theory]
    [InlineData("colegio-x.diariox.online", "colegio-x")]
    [InlineData("COLEGIO-X.DiarioX.Online", "colegio-x")]
    [InlineData("rede1.dev.localhost", "rede1")]
    [InlineData("colegio-x.diariox.online.", "colegio-x")]
    public void GetTenantSlug_WhenHostIsSubdomainOfBaseDomain_ReturnsSlug(string host, string expected)
    {
        var resolver = BuildResolver("diariox.online", "dev.localhost");

        Assert.Equal(expected, resolver.GetTenantSlug(host));
    }

    [Theory]
    [InlineData("diariox.online")]
    [InlineData("admin.diariox.online")]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("outro-dominio.com")]
    [InlineData("colegio-x.diariox.online.evil.com")]
    public void GetTenantSlug_WhenHostIsGlobal_ReturnsNull(string host)
    {
        var resolver = BuildResolver("diariox.online", "dev.localhost");

        Assert.Null(resolver.GetTenantSlug(host));
    }

    [Fact]
    public void GetTenantSlug_PrefersMostSpecificBaseDomain()
    {
        var resolver = BuildResolver("localhost", "dev.localhost");

        Assert.Equal("rede1", resolver.GetTenantSlug("rede1.dev.localhost"));
        Assert.Equal("rede2", resolver.GetTenantSlug("rede2.localhost"));
    }

    [Fact]
    public void GetTenantSlug_WhenNoBaseDomainConfigured_ReturnsNull()
    {
        var resolver = BuildResolver();

        Assert.Null(resolver.GetTenantSlug("colegio-x.diariox.online"));
    }

    private static TenantHostResolver BuildResolver(params string[] baseDomains)
    {
        var settings = new Dictionary<string, string?> { ["Tenancy:AdminSubdomain"] = "admin" };
        for (var i = 0; i < baseDomains.Length; i++)
            settings[$"Tenancy:BaseDomains:{i}"] = baseDomains[i];

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new TenantHostResolver(configuration);
    }
}
