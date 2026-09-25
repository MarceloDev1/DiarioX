using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerfisController : ControllerBase
{
    private readonly IPerfilRepository _perfilRepository;

    public PerfisController(IPerfilRepository perfilRepository)
    {
        _perfilRepository = perfilRepository;
    }

    /// <summary>
    /// Perfis atribuíveis a usuários da instituição. O Administrador é exclusivo dos usuários globais.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var perfis = await _perfilRepository.GetAllAsync();
        return Ok(perfis.Where(p => !string.Equals(p.Nome, Perfil.Administrador, StringComparison.OrdinalIgnoreCase)));
    }
}
