namespace DiarioX.Server.Application.Auth;

/// <summary>
/// Claims próprias do DiarioX emitidas no JWT.
/// </summary>
public static class AppClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string TenantSlug = "tenant_slug";
    public const string GlobalAdmin = "global_admin";
}
