using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Dashboard;

namespace DiarioX.Server.Application.Interfaces;

public interface IDashboardService
{
    /// <summary>Painel da página inicial com os blocos que o perfil do usuário pode ver.</summary>
    Task<DashboardResponse> ObterAsync(UsuarioAtual usuario, DateOnly hoje);
}
