namespace DiarioX.Server.Application.DTOs.Tenants;

public enum TenantResultError
{
    None,
    Validation,
    Conflict,
    NotFound
}

public record TenantCommandResult(
    bool Success,
    string Message,
    TenantResponse? Tenant = null,
    TenantResultError Error = TenantResultError.None
);
