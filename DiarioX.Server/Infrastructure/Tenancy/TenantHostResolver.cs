namespace DiarioX.Server.Infrastructure.Tenancy;

/// <summary>
/// Extrai o slug da instituição do host da requisição a partir dos domínios base
/// configurados em Tenancy:BaseDomains. Ex.: com base "diariox.online",
/// "colegio-x.diariox.online" → "colegio-x".
/// O próprio domínio base, o subdomínio de administração (Tenancy:AdminSubdomain),
/// IPs e hosts fora dos domínios base pertencem à área global.
/// </summary>
public class TenantHostResolver
{
    private readonly string[] _baseDomains;
    private readonly string _adminSubdomain;

    public TenantHostResolver(IConfiguration configuration)
    {
        // Do mais específico para o mais genérico (ex.: "dev.localhost" antes de "localhost").
        _baseDomains = (configuration.GetSection("Tenancy:BaseDomains").Get<string[]>() ?? [])
            .Select(d => d.Trim().Trim('.').ToLowerInvariant())
            .Where(d => d.Length > 0)
            .OrderByDescending(d => d.Length)
            .ToArray();

        _adminSubdomain = (configuration["Tenancy:AdminSubdomain"] ?? "admin").Trim().ToLowerInvariant();
    }

    public string? GetTenantSlug(string host)
    {
        var normalizedHost = host.Trim().TrimEnd('.').ToLowerInvariant();

        foreach (var baseDomain in _baseDomains)
        {
            if (!normalizedHost.EndsWith("." + baseDomain, StringComparison.Ordinal))
                continue;

            var subdomain = normalizedHost[..^(baseDomain.Length + 1)];
            return subdomain == _adminSubdomain ? null : subdomain;
        }

        return null;
    }
}
