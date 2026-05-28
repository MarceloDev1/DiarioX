using DiarioX.Server.Application.DTOs.Auth;

namespace DiarioX.Server.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<FirstAccessOperationResponse> ValidateFirstAccessAsync(FirstAccessValidationRequest request);
    Task<FirstAccessOperationResponse> ActivateFirstAccessAsync(FirstAccessActivationRequest request);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ForgotPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
}
