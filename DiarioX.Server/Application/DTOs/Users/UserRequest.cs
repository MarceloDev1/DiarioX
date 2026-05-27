namespace DiarioX.Server.Application.DTOs.Users;

public class UserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
    public string? Senha { get; set; }
    public string Status { get; set; } = "ATIVO";
    public int? PerfilId { get; set; }
}
