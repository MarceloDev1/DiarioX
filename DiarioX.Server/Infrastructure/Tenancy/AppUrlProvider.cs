using DiarioX.Server.Application.Interfaces;

namespace DiarioX.Server.Infrastructure.Tenancy;

/// <summary>
/// Monta a URL do front-end: Tenancy:AppUrlTemplate com o slug da instituição atual
/// (ex.: "https://{slug}.diariox.online") ou AppUrl na área global.
/// </summary>
public class AppUrlProvider : IAppUrlProvider
{
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;

    public AppUrlProvider(ITenantContext tenantContext, IConfiguration configuration)
    {
        _tenantContext = tenantContext;
        _configuration = configuration;
    }

    public string GetAppUrl()
    {
        var template = _configuration["Tenancy:AppUrlTemplate"];
        if (_tenantContext.TenantSlug is { } slug && !string.IsNullOrWhiteSpace(template))
            return template.Replace("{slug}", slug).TrimEnd('/');

        var appUrl = _configuration["AppUrl"];
        return string.IsNullOrWhiteSpace(appUrl) ? "https://localhost:5173" : appUrl.TrimEnd('/');
    }
}
