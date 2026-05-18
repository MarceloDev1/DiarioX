namespace DiarioX.Server.Application.DTOs.Auth;

public record LoginResponse(string Token, string Username, DateTime ExpiresAt);
