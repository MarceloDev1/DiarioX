using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DiarioX.Server.Application.DTOs.Auth.Validation;

namespace DiarioX.Server.Application.DTOs.Auth;

public sealed class LoginRequest : IValidatableObject
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    public string? GetEffectiveLogin()
    {
        return string.IsNullOrWhiteSpace(Login) ? Username?.Trim() : Login.Trim();
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var effectiveLogin = GetEffectiveLogin();

        if (string.IsNullOrWhiteSpace(effectiveLogin))
            yield return new ValidationResult("Login é obrigatório.", [nameof(Login), nameof(Username)]);
        else if (!new LoginIdentifierAttribute().IsValid(effectiveLogin))
            yield return new ValidationResult("Login deve ser um e-mail valido ou CPF com 11 digitos.", [nameof(Login), nameof(Username)]);

        if (string.IsNullOrWhiteSpace(Password))
            yield return new ValidationResult("Senha é obrigatória.", [nameof(Password)]);
    }
}
