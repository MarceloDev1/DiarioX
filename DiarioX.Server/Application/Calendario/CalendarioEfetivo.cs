using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Application.Calendario;

/// <summary>
/// Regras do Calendário Letivo que valem para a escola (RF005A): os eventos da rede, substituídos pelos da
/// escola nos mesmos dias. Sem evento, segunda a sexta é dia letivo e sábado/domingo não.
/// </summary>
public static class CalendarioEfetivo
{
    /// <summary>Eventos por dia: os da rede e, por cima, os da escola.</summary>
    public static Dictionary<DateOnly, EventoCalendario> Mesclar(CalendarioLetivo? rede, CalendarioLetivo? escola)
    {
        var eventos = new Dictionary<DateOnly, EventoCalendario>();
        foreach (var evento in (rede?.Eventos ?? []).Concat(escola?.Eventos ?? []))
            eventos[evento.Data] = evento;
        return eventos;
    }

    public static bool DiaLetivo(DateOnly data, EventoCalendario? evento)
        => evento?.ComAula ?? CalendarioLetivo.DiaUtil(data);

    /// <summary>RN02: dias letivos entre o início e o término do ano letivo.</summary>
    public static int ContarDiasLetivos(AnoLetivo ano, IReadOnlyDictionary<DateOnly, EventoCalendario> eventos)
    {
        var total = 0;
        for (var dia = ano.DataInicio; dia <= ano.DataTermino; dia = dia.AddDays(1))
        {
            if (DiaLetivo(dia, eventos.GetValueOrDefault(dia)))
                total++;
        }
        return total;
    }

    /// <summary>
    /// RN01/EX01: motivo do bloqueio do diário de classe na data, ou nulo se os lançamentos são permitidos.
    /// Sem calendário publicado (da rede ou da escola) não há trava.
    /// </summary>
    public static string? MotivoBloqueio(DateOnly data, IReadOnlyCollection<CalendarioLetivo> publicados)
    {
        if (!Bloqueado(data, publicados, out var evento))
            return null;

        if (evento is not null)
        {
            return "Não é possível realizar lançamentos nesta data. Evento cadastrado no Calendário Escolar: " +
                   $"{evento.Descricao} - Dia Sem Aula.";
        }

        return $"Não é possível realizar lançamentos nesta data: {NomeDoDia(data)} sem Dia Letivo Especial (Sábado Letivo) " +
               "cadastrado no Calendário Escolar.";
    }

    /// <summary>RF017 EX01: o mesmo bloqueio, com o texto da tela de frequência.</summary>
    public static string? MotivoBloqueioFrequencia(DateOnly data, IReadOnlyCollection<CalendarioLetivo> publicados)
    {
        if (!Bloqueado(data, publicados, out var evento))
            return null;

        var descricao = evento is not null
            ? evento.Descricao
            : $"{NomeDoDia(data)} sem Dia Letivo Especial (Sábado Letivo)";
        return $"Não é possível registrar frequência. Data configurada como {descricao} no Calendário Escolar.";
    }

    private static bool Bloqueado(DateOnly data, IReadOnlyCollection<CalendarioLetivo> publicados, out EventoCalendario? evento)
    {
        evento = null;
        if (publicados.Count == 0)
            return false;

        evento = Mesclar(
            publicados.FirstOrDefault(c => c.EscolaId is null),
            publicados.FirstOrDefault(c => c.EscolaId is not null)).GetValueOrDefault(data);

        return !DiaLetivo(data, evento);
    }

    private static string NomeDoDia(DateOnly data) => data.DayOfWeek == DayOfWeek.Saturday ? "sábado" : "domingo";
}
