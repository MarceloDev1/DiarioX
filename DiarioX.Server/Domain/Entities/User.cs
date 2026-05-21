namespace DiarioX.Server.Domain.Entities;

public class User
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";
    public const string StatusBloqueado = "BLOQUEADO";

    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Status { get; set; } = StatusAtivo;
    public DateTime? UltimoAcesso { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, SenhaHash);
    }

    public void SetPassword(string password)
    {
        SenhaHash = BCrypt.Net.BCrypt.HashPassword(password);
    }
}
