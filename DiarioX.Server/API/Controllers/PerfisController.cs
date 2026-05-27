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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var perfis = await _perfilRepository.GetAllAsync();
        return Ok(perfis);
    }
}
