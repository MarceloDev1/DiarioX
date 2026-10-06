using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Calendario;

namespace DiarioX.Server.Application.Interfaces;

public interface ICalendarioLetivoService
{
    Task<CalendarioOpcoesResponse> GetOpcoesAsync();
    Task<CalendarioCommandResult> GetAsync(int anoLetivoId, int? escolaId);
    Task<CalendarioCommandResult> SalvarEventoAsync(EventoCalendarioRequest request);
    Task<CalendarioCommandResult> RemoverEventosAsync(int anoLetivoId, int? escolaId, DateOnly de, DateOnly? ate);
    Task<CalendarioCommandResult> PublicarAsync(UsuarioAtual usuario, CalendarioPublicacaoRequest request);
}
