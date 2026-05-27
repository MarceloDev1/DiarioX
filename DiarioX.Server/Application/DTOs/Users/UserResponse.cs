namespace DiarioX.Server.Application.DTOs.Users;

public record UserResponse(
    int Id,
    string Email,
    string Cpf,
    DateTime? DataNascimento,
    string Status,
    DateTime? UltimoAcesso,
    DateTime CreatedAt
);
