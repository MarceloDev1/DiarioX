namespace DiarioX.Server.Application.DTOs.Users;

public enum UserResultError
{
    None,
    Validation,
    Conflict,
    NotFound
}

public record UserCommandResult(
    bool Success,
    string Message,
    UserResponse? User = null,
    UserResultError Error = UserResultError.None
);
