using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Dashboard;

namespace DiarioX.Server.Application.Interfaces;

public interface IPainelProfessorService
{
    /// <summary>Home do professor; nulo quando o usuário não tem cadastro de professor nem o perfil Professor.</summary>
    Task<PainelProfessorResponse?> ObterAsync(UsuarioAtual usuario, DateOnly hoje, int? escolaId);

    /// <summary>Calendário de pendências de um mês (navegação para meses anteriores).</summary>
    Task<PendenciasMesResponse?> ObterPendenciasAsync(UsuarioAtual usuario, DateOnly hoje, int? escolaId, int ano, int mes);
}
