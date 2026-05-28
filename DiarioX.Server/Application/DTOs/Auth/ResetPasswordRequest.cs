namespace DiarioX.Server.Application.DTOs.Auth;

public record ResetPasswordRequest(string Token, string Password);
