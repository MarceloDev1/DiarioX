namespace DiarioX.Server.Application.DTOs.Tenants;

public record TenantResponse(
    int Id,
    string Nome,
    string Slug,
    string Status,
    DateTime CreatedAt
);
