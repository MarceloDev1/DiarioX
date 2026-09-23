namespace DiarioX.Server.Domain.Entities;

public class User
{
    public const string StatusAtivo = "ATIVO";
    public const string StatusInativo = "INATIVO";
    public const string StatusBloqueado = "BLOQUEADO";

    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
    public string SenhaHash { get; set; } = string.Empty;
    public string Status { get; set; } = StatusAtivo;
    public DateTime? UltimoAcesso { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<UsuarioPerfil> UsuariosPerfis { get; set; } = new List<UsuarioPerfil>();

    public bool IsLockedOut(DateTime nowUtc) => LockoutEnd.HasValue && LockoutEnd.Value > nowUtc;

    public void RegisterFailedLoginAttempt(DateTime nowUtc)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockoutEnd = nowUtc.Add(LockoutDuration);
            FailedLoginAttempts = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTime nowUtc)
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UltimoAcesso = nowUtc;
    }

    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, SenhaHash);
    }

    public void SetPassword(string password)
    {
        SenhaHash = BCrypt.Net.BCrypt.HashPassword(password);
    }
}
