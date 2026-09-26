using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DiarioX.Server.Application.Auth;

namespace DiarioX.Server.Infrastructure.Authorization;

public static class UsuarioAtualExtensions
{
    public static bool TryGetUsuarioAtual(this ClaimsPrincipal user, out UsuarioAtual usuario)
    {
        usuario = null!;
        if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var usuarioId))
            return false;

        usuario = new UsuarioAtual(usuarioId, user.HasClaim(AppClaimTypes.GlobalAdmin, "true"));
        return true;
    }
}
