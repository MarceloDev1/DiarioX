namespace DiarioX.Server.Application.Faturamento;

/// <summary>Validação de CPF e CNPJ (apenas dígitos) pelos dígitos verificadores.</summary>
public static class Documentos
{
    public static string SomenteDigitos(string? valor)
        => new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool CpfOuCnpjValido(string digitos)
        => digitos.Length switch
        {
            11 => CpfValido(digitos),
            14 => CnpjValido(digitos),
            _ => false,
        };

    public static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || !cpf.All(char.IsDigit) || cpf.Distinct().Count() == 1)
            return false;

        var n = cpf.Select(c => c - '0').ToArray();
        return n[9] == DigitoCpf(n, 9) && n[10] == DigitoCpf(n, 10);
    }

    public static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14 || !cnpj.All(char.IsDigit) || cnpj.Distinct().Count() == 1)
            return false;

        var n = cnpj.Select(c => c - '0').ToArray();
        return n[12] == DigitoCnpj(n, 12) && n[13] == DigitoCnpj(n, 13);
    }

    private static int DigitoCpf(int[] n, int tamanho)
    {
        var soma = 0;
        for (var i = 0; i < tamanho; i++)
            soma += n[i] * (tamanho + 1 - i);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    // Pesos 5..2,9..2 (1º dígito) e 6..2,9..2 (2º dígito).
    private static int DigitoCnpj(int[] n, int tamanho)
    {
        var soma = 0;
        var peso = tamanho - 7;
        for (var i = 0; i < tamanho; i++)
        {
            soma += n[i] * peso;
            peso = peso == 2 ? 9 : peso - 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
