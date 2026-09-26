using DiarioX.Server.Application.Faturamento;
using DiarioX.Server.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DiarioX.Server.Infrastructure.Services;

/// <summary>
/// Executa periodicamente a rotina de faturamento (gerar faturas, reenviar cobranças e aplicar a
/// régua de atraso). A rotina é idempotente, então rodar várias vezes por dia é seguro.
/// </summary>
public class FaturamentoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FaturamentoOptions _options;
    private readonly ILogger<FaturamentoBackgroundService> _logger;

    public FaturamentoBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<FaturamentoOptions> options,
        ILogger<FaturamentoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.RotinaHabilitada)
        {
            _logger.LogInformation("Rotina de faturamento desativada (Faturamento:RotinaHabilitada).");
            return;
        }

        // Aguarda a aplicação terminar de subir (migrations e seed) antes da primeira execução.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(5, _options.IntervaloRotinaMinutos)));
        do
        {
            await ExecutarAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task ExecutarAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IFaturamentoService>();
            var resultado = await service.ExecutarRotinaAsync(DateOnly.FromDateTime(DateTime.Today), ct);

            _logger.LogInformation(
                "Rotina de faturamento: {Geradas} faturas geradas, {Reenviadas} cobranças reenviadas, {Vencidas} vencidas, {Atualizadas} instituições com situação alterada, {Erros} erros",
                resultado.FaturasGeradas, resultado.CobrancasReenviadas, resultado.FaturasVencidas,
                resultado.InstituicoesAtualizadas, resultado.Erros.Count);

            foreach (var erro in resultado.Erros)
                _logger.LogWarning("Faturamento: {Erro}", erro);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // aplicação encerrando
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na rotina de faturamento");
        }
    }
}
