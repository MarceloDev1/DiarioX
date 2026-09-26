using DiarioX.Server.Application.DTOs.Faturamento;
using DiarioX.Server.Application.Faturamento;

namespace DiarioX.Server.Application.Interfaces;

/// <summary>Faturamento da plataforma: cobrança das instituições pelo uso do Diário X.</summary>
public interface IFaturamentoService
{
    // Administrador global
    Task<PainelFaturamentoResponse> ObterPainelAsync(DateOnly hoje);
    Task<IEnumerable<PlanoResponse>> ListarPlanosAsync();
    Task<FaturamentoResult<PlanoResponse>> SalvarPlanoAsync(int? id, PlanoRequest request);
    Task<IEnumerable<AssinaturaResumoResponse>> ListarAssinaturasAsync(DateOnly hoje);
    Task<FaturamentoResult<AssinaturaResponse>> ObterAssinaturaAsync(int tenantId);
    Task<FaturamentoResult<AssinaturaResponse>> SalvarAssinaturaAsync(int tenantId, AssinaturaRequest request);
    Task<IEnumerable<FaturaResponse>> ListarFaturasAsync(int? tenantId, string? situacao, DateOnly hoje);
    Task<FaturamentoResult<FaturaResponse>> GerarFaturaAsync(int tenantId, GerarFaturaRequest request, DateOnly hoje);
    Task<FaturamentoResult<FaturaResponse>> RegistrarPagamentoManualAsync(int faturaId, PagamentoManualRequest request, DateOnly hoje);
    Task<FaturamentoResult<FaturaResponse>> CancelarFaturaAsync(int faturaId, DateOnly hoje);
    Task<FaturamentoResult<FaturaResponse>> EmitirNotaFiscalAsync(int faturaId, DateOnly hoje);

    /// <summary>Gera as faturas do período, reenvia cobranças pendentes e aplica a régua de atraso.</summary>
    Task<RotinaFaturamentoResponse> ExecutarRotinaAsync(DateOnly hoje, CancellationToken ct = default);

    /// <summary>Processa um evento do webhook do Asaas (idempotente).</summary>
    Task ProcessarEventoAsync(AsaasEvento evento, DateOnly hoje);

    // Instituição
    Task<AvisoFinanceiroResponse> ObterAvisoAsync(int tenantId, bool podeVerFaturas, DateOnly hoje);
    Task<MinhaAssinaturaResponse?> ObterMinhaAssinaturaAsync(int tenantId);
}
