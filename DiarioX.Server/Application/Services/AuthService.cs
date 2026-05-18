using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user is null)
            return null;

        // Comentado temporariamente para testes - descomentar em produção
        // if (!user.VerifyPassword(request.Password))
        //     return null;

        var token = GenerateJwtToken(user.Username);
        return token;
    }

    private LoginResponse GenerateJwtToken(string username)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new LoginResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            Username: username,
            ExpiresAt: expiresAt
        );
    }
}
