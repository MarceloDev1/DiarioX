using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DiarioX.Server.Application.DTOs.Auth;

public class FirstAccessValidationRequest
{
    [Required(ErrorMessage = "CPF e obrigatorio.")]
    [JsonPropertyName("cpf")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data de nascimento e obrigatoria.")]
    [JsonPropertyName("birthDate")]
    public DateTime? BirthDate { get; set; }
}
