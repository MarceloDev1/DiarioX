using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DiarioX.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    // Usuários em memória — substitua por banco de dados futuramente.
    // As senhas devem ser armazenadas com hash (bcrypt/Argon2) em produção.
    private static readonly IReadOnlyList<(string Username, string PasswordHash)> Users =
    [
        ("admin", BCrypt.Net.BCrypt.HashPassword("admin123")),
    ];

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Usuário e senha são obrigatórios." });

        var user = Users.FirstOrDefault(u =>
            string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase));

        if (user == default || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Usuário ou senha inválidos." });

        var token = GenerateJwtToken(user.Username);
        return Ok(token);
    }

    private LoginResponse GenerateJwtToken(string username)
    {
        var jwtSettings = configuration.GetSection("Jwt");
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
