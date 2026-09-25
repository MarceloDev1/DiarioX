using DiarioX.Server.Application.DTOs.Auth;

namespace DiarioX.Server.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<FirstAccessOperationResponse> ValidateFirstAccessAsync(FirstAccessValidationRequest request);
    Task<FirstAccessOperationResponse> ActivateFirstAccessAsync(FirstAccessActivationRequest request);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ForgotPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Emite um novo token para o Administrador global atuar na instituição informada.
    /// Retorna null se o usuário não for global ou a instituição não existir ou estiver inativa.
    /// </summary>
    Task<LoginResponse?> SelectTenantAsync(int userId, int tenantId);
}
