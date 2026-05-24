using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);

        if (response is null)
            return Unauthorized(new { message = "Usuário ou senha inválidos." });

        return Ok(response);
    }

    [HttpPost("first-access/validate")]
    public async Task<IActionResult> ValidateFirstAccess([FromBody] FirstAccessValidationRequest request)
    {
        var response = await _authService.ValidateFirstAccessAsync(request);

        if (!response.Success)
            return Unauthorized(new { message = response.Message });

        return Ok(response);
    }

    [HttpPost("first-access/activate")]
    public async Task<IActionResult> ActivateFirstAccess([FromBody] FirstAccessActivationRequest request)
    {
        var response = await _authService.ActivateFirstAccessAsync(request);

        if (!response.Success)
            return BadRequest(new { message = response.Message });

        return Ok(response);
    }
}
