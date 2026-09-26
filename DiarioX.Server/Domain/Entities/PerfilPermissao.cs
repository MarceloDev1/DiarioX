namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Permissão concedida a um perfil dentro de uma instituição. O código segue o formato
/// "modulo.acao" (ex.: "alunos.criar"); o catálogo válido fica em Application/Auth/Permissoes.
/// </summary>
public class PerfilPermissao : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
    public string Permissao { get; set; } = string.Empty;
}
