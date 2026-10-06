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
        if (publicados.Count == 0)
            return null;

        var evento = Mesclar(
            publicados.FirstOrDefault(c => c.EscolaId is null),
            publicados.FirstOrDefault(c => c.EscolaId is not null)).GetValueOrDefault(data);

        if (DiaLetivo(data, evento))
            return null;

        if (evento is not null)
        {
            return "Não é possível realizar lançamentos nesta data. Evento cadastrado no Calendário Escolar: " +
                   $"{evento.Descricao} - Dia Sem Aula.";
        }

        var dia = data.DayOfWeek == DayOfWeek.Saturday ? "sábado" : "domingo";
        return $"Não é possível realizar lançamentos nesta data: {dia} sem Dia Letivo Especial (Sábado Letivo) " +
               "cadastrado no Calendário Escolar.";
    }
}
