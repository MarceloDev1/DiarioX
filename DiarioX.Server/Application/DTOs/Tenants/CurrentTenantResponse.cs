namespace DiarioX.Server.Application.DTOs.Tenants;

/// <summary>
/// Dados públicos da instituição do subdomínio, exibidos na tela de login.
/// </summary>
public record CurrentTenantResponse(string Nome, string Slug);
