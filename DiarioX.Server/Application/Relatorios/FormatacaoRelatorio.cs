using System.Globalization;
using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.Relatorios;

/// <summary>Formatação em pt-BR compartilhada pelos relatórios e pela exportação em PDF.</summary>
public static class FormatacaoRelatorio
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly TimeZoneInfo? FusoBrasilia = BuscarFusoBrasilia();

    /// <summary>Data e hora de Brasília, para a emissão dos documentos (o servidor roda em UTC).</summary>
    public static DateTime AgoraEmBrasilia()
        => FusoBrasilia is null ? DateTime.Now : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasilia);

    public static string Formatar(object? valor, string tipo) => valor switch
    {
        null => "—",
        DateOnly data => data.ToString("dd/MM/yyyy", PtBr),
        DateTime dataHora => dataHora.ToString("dd/MM/yyyy", PtBr),
        decimal numero when tipo == TiposColuna.Percentual => Percentual(numero),
        decimal numero => numero.ToString("N2", PtBr),
        int numero => numero.ToString("N0", PtBr),
        _ => Convert.ToString(valor, PtBr) ?? string.Empty,
    };

    public static string Percentual(decimal valor) => $"{valor.ToString("N1", PtBr)}%";

    public static string Inteiro(int valor) => valor.ToString("N0", PtBr);

    public static string Turno(string turno) => turno switch
    {
        Turma.TurnoManha => "Manhã",
        Turma.TurnoTarde => "Tarde",
        Turma.TurnoNoite => "Noite",
        Turma.TurnoIntegral => "Integral",
        _ => turno,
    };

    public static string Sexo(string sexo) => sexo switch
    {
        Aluno.SexoMasculino => "Masculino",
        Aluno.SexoFeminino => "Feminino",
        _ => sexo,
    };

    public static string Telefone(string digitos) => digitos.Length switch
    {
        11 => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}",
        10 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
        _ => digitos,
    };

    public static int Idade(DateTime nascimento, DateOnly hoje)
    {
        var idade = hoje.Year - nascimento.Year;
        if (DateOnly.FromDateTime(nascimento) > hoje.AddYears(-idade))
            idade--;
        return idade;
    }

    private static TimeZoneInfo? BuscarFusoBrasilia()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }
}
