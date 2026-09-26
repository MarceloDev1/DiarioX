namespace DiarioX.Server.Application.Auth;

/// <summary>Usuário da requisição, extraído do token (sub e global_admin).</summary>
public record UsuarioAtual(int UsuarioId, bool IsGlobalAdmin);
