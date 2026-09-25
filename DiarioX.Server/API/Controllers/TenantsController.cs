using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Tenants;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

/// <summary>
/// Gestão das instituições (tenants), restrita ao Administrador global.
/// </summary>
[ApiController]
[Authorize(Policy = AppPolicies.GlobalAdmin)]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Instituição do subdomínio acessado, para exibir na tela de login.
    /// Retorna 204 no host de administração.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var tenant = await _tenantService.GetCurrentAsync();
        return tenant is null ? NoContent() : Ok(tenant);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _tenantService.GetAllAsync();
        return Ok(tenants);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var tenant = await _tenantService.GetByIdAsync(id);
        if (tenant is null)
            return NotFound(new { message = "Instituição não encontrada." });

        return Ok(tenant);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TenantRequest request)
    {
        var result = await _tenantService.CreateAsync(request);
        if (!result.Success)
            return MapError(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Tenant!.Id }, result.Tenant);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TenantRequest request)
    {
        var result = await _tenantService.UpdateAsync(id, request);
        if (!result.Success)
            return MapError(result);

        return Ok(result.Tenant);
    }

    private IActionResult MapError(TenantCommandResult result)
    {
        return result.Error switch
        {
            TenantResultError.NotFound => NotFound(new { message = result.Message }),
            TenantResultError.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }
}
