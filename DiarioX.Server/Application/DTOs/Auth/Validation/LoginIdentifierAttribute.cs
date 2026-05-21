using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DiarioX.Server.Application.DTOs.Auth.Validation;

public sealed class LoginIdentifierAttribute : ValidationAttribute
{
    private static readonly Regex DigitsOnly = new("^\\d{11}$", RegexOptions.Compiled);

    public LoginIdentifierAttribute()
    {
        ErrorMessage = "Login deve ser um e-mail valido ou CPF com 11 digitos.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string rawLogin)
            return false;

        var login = rawLogin.Trim();
        if (string.IsNullOrWhiteSpace(login))
            return false;

        if (login.Contains('@'))
            return new EmailAddressAttribute().IsValid(login);

        var normalizedCpf = Regex.Replace(login, "\\D", string.Empty);
        return DigitsOnly.IsMatch(normalizedCpf);
    }
}
