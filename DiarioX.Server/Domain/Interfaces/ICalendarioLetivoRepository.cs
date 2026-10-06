using DiarioX.Server.Domain.Entities;

namespace DiarioX.Server.Domain.Interfaces;

public interface ICalendarioLetivoRepository
{
    /// <summary>Calendário do ano letivo (com os eventos): o da escola ou, com escolaId nulo, o da rede.</summary>
    Task<CalendarioLetivo?> GetAsync(int anoLetivoId, int? escolaId);

    /// <summary>
    /// Calendários publicados que valem para a escola no ano letivo: o da rede e o da própria escola (com os
    /// eventos). Ignora o escopo de escola do usuário: quem chega aqui já teve o acesso à turma validado.
    /// </summary>
    Task<IReadOnlyList<CalendarioLetivo>> GetPublicadosAsync(int anoLetivoId, int escolaId);

    /// <summary>Calendário existente ou um novo, ainda não publicado.</summary>
    Task<CalendarioLetivo> GetOrCreateAsync(int anoLetivoId, int? escolaId);

    /// <summary>Troca os eventos do calendário no intervalo [de, ate] pelos informados (nenhum = remove).</summary>
    Task SubstituirEventosAsync(int calendarioId, DateOnly de, DateOnly ate, IEnumerable<EventoCalendario> eventos);

    Task PublicarAsync(int calendarioId, int usuarioId);
}
