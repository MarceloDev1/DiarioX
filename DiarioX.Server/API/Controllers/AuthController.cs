using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            var message = result.FailureReason switch
            {
                LoginFailureReason.UserBlocked =>
                    "Acesso negado. Usuário Bloqueado no sistema. Entre em contato com a secretaria/suporte.",
                LoginFailureReason.UserInactive =>
                    "Acesso negado. Usuário inativo no sistema. Use o primeiro acesso para ativar sua conta.",
                LoginFailureReason.AccountLocked =>
                    "Conta bloqueada temporariamente devido a múltiplas tentativas. Tente novamente mais tarde ou recupere sua senha.",
                _ => "Usuário ou senha inválidos.",
            };

            return Unauthorized(new { message });
        }

        return Ok(result.Response);
    }

    /// <summary>
    /// Administrador global escolhe a instituição em que vai atuar; devolve um novo token com ela.
    /// </summary>
    [Authorize(Policy = AppPolicies.GlobalAdmin)]
    [HttpPost("select-tenant")]
    public async Task<IActionResult> SelectTenant([FromBody] SelectTenantRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Unauthorized();

        var response = await _authService.SelectTenantAsync(userId, request.TenantId);
        if (response is null)
            return NotFound(new { message = "Instituição não encontrada ou inativa." });

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("first-access/validate")]
    public async Task<IActionResult> ValidateFirstAccess([FromBody] FirstAccessValidationRequest request)
    {
        var response = await _authService.ValidateFirstAccessAsync(request);

        if (!response.Success)
            return Unauthorized(new { message = response.Message });

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("first-access/activate")]
    public async Task<IActionResult> ActivateFirstAccess([FromBody] FirstAccessActivationRequest request)
    {
        var response = await _authService.ActivateFirstAccessAsync(request);

        if (!response.Success)
            return BadRequest(new { message = response.Message });

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var response = await _authService.ForgotPasswordAsync(request);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var response = await _authService.ResetPasswordAsync(request);

        if (!response.Success)
            return BadRequest(new { message = response.Message });

        return Ok(response);
    }

    [AllowAnonymous]
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
