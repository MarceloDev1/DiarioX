namespace DiarioX.Server.Domain.Entities;

/// <summary>
/// Instituição ou mantenedora (rede) que contrata o sistema. Agrupa uma ou mais escolas
/// e isola todos os dados cadastrados por ela.
/// </summary>
public class Tenant
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";

    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    // Subdomínio de acesso da instituição (ex.: "colegio-x" em colegio-x.diariox.online).
    public string Slug { get; set; } = string.Empty;

    public string Status { get; set; } = StatusAtivo;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
