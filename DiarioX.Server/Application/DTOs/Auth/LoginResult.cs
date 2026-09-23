namespace DiarioX.Server.Application.DTOs.Auth;

public enum LoginFailureReason
{
    InvalidCredentials,
    UserBlocked,
    UserInactive,
    AccountLocked,
}

public sealed class LoginResult
{
    public LoginResponse? Response { get; }
    public LoginFailureReason? FailureReason { get; }
    public bool Success => Response is not null;

    private LoginResult(LoginResponse? response, LoginFailureReason? failureReason)
    {
        Response = response;
        FailureReason = failureReason;
    }

    public static LoginResult Ok(LoginResponse response) => new(response, null);

    public static LoginResult Fail(LoginFailureReason reason) => new(null, reason);
}
