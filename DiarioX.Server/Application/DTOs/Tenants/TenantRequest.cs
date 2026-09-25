namespace DiarioX.Server.Application.DTOs.Tenants;

public class TenantRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "ATIVO";
}
