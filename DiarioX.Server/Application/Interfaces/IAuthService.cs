using DiarioX.Server.Application.DTOs.Auth;

namespace DiarioX.Server.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
