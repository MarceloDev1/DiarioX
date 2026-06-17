using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DiarioX.Server.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IEmailService emailService, IWebHostEnvironment env)
    {
        _authService = authService;
        _emailService = emailService;
        _env = env;
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var response = await _authService.ForgotPasswordAsync(request);
        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var response = await _authService.ResetPasswordAsync(request);

        if (!response.Success)
            return BadRequest(new { message = response.Message });

        return Ok(response);
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] string toEmail)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        try
        {
            await _emailService.SendAsync(toEmail, null, "Teste SMTP - Diário de Classe", "<p>Configuração SMTP funcionando corretamente.</p>");
            return Ok(new { message = $"E-mail enviado para {toEmail}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, detail = ex.ToString() });
        }
    }
}
