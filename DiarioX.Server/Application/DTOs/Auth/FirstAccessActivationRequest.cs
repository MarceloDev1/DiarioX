using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DiarioX.Server.Application.DTOs.Auth;

public sealed class FirstAccessActivationRequest : FirstAccessValidationRequest
{
    [Required(ErrorMessage = "E-mail e obrigatorio.")]
    [EmailAddress(ErrorMessage = "E-mail invalido.")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha e obrigatoria.")]
    [MinLength(8, ErrorMessage = "A senha deve ter no minimo 8 caracteres.")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
