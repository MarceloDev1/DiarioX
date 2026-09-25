namespace DiarioX.Server.Application.DTOs.Auth;

public record LoginResponse(
    string Token,
    string Email,
    DateTime ExpiresAt,
    int? TenantId = null,
    string? TenantNome = null,
    bool IsGlobalAdmin = false);
