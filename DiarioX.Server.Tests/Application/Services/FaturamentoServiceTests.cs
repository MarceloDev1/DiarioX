using DiarioX.Server.Application.DTOs.Faturamento;
using DiarioX.Server.Application.Faturamento;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using DiarioX.Server.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiarioX.Server.Tests.Application.Services;

/// <summary>
/// Regras do faturamento da plataforma com o repositório real (InMemory, sem instituição na
/// requisição, como a rotina em segundo plano) e um Asaas falso.
/// </summary>
public class FaturamentoServiceTests
{
    // Colégio com 30 alunos ativos; plano R$ 100 + R$ 2/aluno (mínimo R$ 150); 10% de desconto; vencimento dia 10.
    private static readonly DateOnly Vencimento = new(2026, 10, 10);

    [Theory]
    [InlineData(0, 150)]      // piso mínimo
    [InlineData(30, 160)]     // 100 + 30 × 2
    [InlineData(200, 500)]    // 100 + 200 × 2
    public void Plano_CalculaValorComPisoMinimo(int alunos, decimal esperado)
    {
        var plano = new PlanoAssinatura { ValorFixo = 100, ValorPorAluno = 2, ValorMinimo = 150 };

        Assert.Equal(esperado, plano.CalcularValor(alunos));
    }

    [Fact]
    public async Task Rotina_GeraFaturaNaJanelaEEnviaAoAsaas_SemDuplicar()
    {
        var f = new Fixture();

        var primeira = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));
        var segunda = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));

        Assert.Equal(1, primeira.FaturasGeradas);
        Assert.Equal(0, segunda.FaturasGeradas);
        var fatura = Assert.Single(f.Faturas());
        Assert.Equal((30, 160m, 144m), (fatura.AlunosAtivos, fatura.ValorCalculado, fatura.Valor));
        Assert.Equal(Vencimento, fatura.Vencimento);
        Assert.Equal("pay_1", fatura.AsaasCobrancaId);
        var cobranca = Assert.Single(f.Asaas.Cobrancas);
        Assert.Equal((144m, Vencimento, $"fatura-{fatura.Id}"), (cobranca.Valor, cobranca.Vencimento, cobranca.ReferenciaExterna));
        Assert.Equal("cus_1", f.AssinaturaAtual().AsaasClienteId);
    }

    [Fact]
    public async Task Rotina_ForaDaJanelaOuEmTeste_NaoGeraFatura()
    {
        var f = new Fixture(testeAte: new DateOnly(2026, 10, 15));

        var foraDaJanela = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 9, 25));
        var emTeste = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));
        var aposTeste = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 16));

        Assert.Equal(0, foraDaJanela.FaturasGeradas + emTeste.FaturasGeradas + aposTeste.FaturasGeradas);
        Assert.Equal(Assinatura.SituacaoAtiva, f.AssinaturaAtual().Situacao);
    }

    [Theory]
    [InlineData(11, "EM_ATRASO")]
    [InlineData(19, "EM_ATRASO")]
    [InlineData(20, "SOMENTE_LEITURA")]
    public async Task Regua_AplicaSituacaoPelosDiasDeAtraso(int dia, string esperada)
    {
        var f = new Fixture();
        await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));

        await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, dia));

        Assert.Equal(FaturaAssinatura.SituacaoVencida, Assert.Single(f.Faturas()).Situacao);
        Assert.Equal(esperada, f.Instituicao().SituacaoFinanceira);
    }

    [Fact]
    public async Task Webhook_PagamentoLiberaInstituicaoEEmiteNotaFiscal_UmaVez()
    {
        var f = new Fixture(notaFiscal: true);
        await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));
        await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 25));
        Assert.Equal(Tenant.SituacaoFinanceiraSomenteLeitura, f.Instituicao().SituacaoFinanceira);

        var pagamento = new AsaasEvento("PAYMENT_RECEIVED",
            new AsaasEventoCobranca("pay_1", "RECEIVED", 147.5m, "PIX", new DateOnly(2026, 10, 25), null), null);
        await f.Service.ProcessarEventoAsync(pagamento, new DateOnly(2026, 10, 25));
        await f.Service.ProcessarEventoAsync(pagamento, new DateOnly(2026, 10, 25));
        await f.Service.ProcessarEventoAsync(new AsaasEvento("INVOICE_AUTHORIZED", null,
            new AsaasEventoNotaFiscal("inv_1", "AUTHORIZED", "2026123", "https://nf/2026123.pdf", "pay_1", null)),
            new DateOnly(2026, 10, 25));

        var fatura = Assert.Single(f.Faturas());
        Assert.Equal((FaturaAssinatura.SituacaoPaga, 147.5m, "PIX"), (fatura.Situacao, fatura.ValorPago, fatura.FormaPagamento));
        Assert.Equal(Tenant.SituacaoFinanceiraRegular, f.Instituicao().SituacaoFinanceira);
        Assert.Single(f.Asaas.NotasFiscais);
        Assert.Equal((FaturaAssinatura.NotaEmitida, "2026123"), (fatura.NotaFiscalSituacao, fatura.NotaFiscalNumero));
    }

    [Fact]
    public async Task FaturaCancelada_NaoVoltaNaRotina_MasPodeSerGeradaManualmente()
    {
        var f = new Fixture();
        await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));
        var faturaId = Assert.Single(f.Faturas()).Id;

        var cancelada = await f.Service.CancelarFaturaAsync(faturaId, new DateOnly(2026, 10, 2));
        var rotina = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 3));
        var manual = await f.Service.GerarFaturaAsync(Fixture.TenantId, new GerarFaturaRequest(), new DateOnly(2026, 10, 3));

        Assert.True(cancelada.Success, cancelada.Message);
        Assert.Equal(["pay_1"], f.Asaas.Canceladas);
        Assert.Equal(0, rotina.FaturasGeradas);
        Assert.True(manual.Success, manual.Message);
        Assert.Equal(2, f.Faturas().Count);
    }

    [Fact]
    public async Task SemAsaas_GeraFaturaLocalEAceitaPagamentoManual()
    {
        var f = new Fixture(asaasConfigurado: false);
        await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));
        var fatura = Assert.Single(f.Faturas());

        var pagamento = await f.Service.RegistrarPagamentoManualAsync(fatura.Id,
            new PagamentoManualRequest { Data = new DateOnly(2026, 10, 5) }, new DateOnly(2026, 10, 5));

        Assert.Null(fatura.AsaasCobrancaId);
        Assert.Contains("Asaas não configurado", fatura.Observacao);
        Assert.True(pagamento.Success, pagamento.Message);
        Assert.Equal((FaturaAssinatura.SituacaoPaga, 144m, "MANUAL"), (fatura.Situacao, fatura.ValorPago, fatura.FormaPagamento));
        Assert.Empty(f.Asaas.Cobrancas);
    }

    [Fact]
    public async Task ErroNoAsaas_FicaRegistradoEAProximaRotinaReenvia()
    {
        var f = new Fixture();
        f.Asaas.FalharCobranca = "Asaas: O campo cpfCnpj é inválido.";

        var comErro = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 1));
        var fatura = Assert.Single(f.Faturas());
        Assert.Null(fatura.AsaasCobrancaId);
        Assert.Contains("cpfCnpj", fatura.Observacao);
        Assert.Single(comErro.Erros);

        f.Asaas.FalharCobranca = null;
        var reenvio = await f.Service.ExecutarRotinaAsync(new DateOnly(2026, 10, 2));

        Assert.Equal(1, reenvio.CobrancasReenviadas);
        Assert.Equal("pay_1", fatura.AsaasCobrancaId);
        Assert.Null(fatura.Observacao);
    }

    [Fact]
    public async Task SalvarAssinatura_ValidaDocumentoDoTomador()
    {
        var f = new Fixture();

        var result = await f.Service.SalvarAssinaturaAsync(Fixture.TenantId, new AssinaturaRequest
        {
            PlanoId = f.AssinaturaAtual().PlanoId, Situacao = "ATIVA", DataInicio = new DateOnly(2026, 1, 1),
            DiaVencimento = 10, RazaoSocial = "Colégio A", CpfCnpj = "11.111.111/1111-11", Email = "financeiro@colegio.com",
        });

        Assert.False(result.Success);
        Assert.Equal(FaturamentoErro.Validation, result.Error);
    }

    private sealed class Fixture
    {
        public const int TenantId = 1;

        private readonly AppDbContext _context;

        public FakeAsaas Asaas { get; }
        public FaturamentoService Service { get; }

        public Fixture(DateOnly? testeAte = null, bool notaFiscal = false, bool asaasConfigurado = true)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"faturamento-{Guid.NewGuid()}")
                .Options;
            _context = new AppDbContext(options, new SemInstituicao());

            var instituicao = new Tenant { Id = TenantId, Nome = "Colégio A", Slug = "colegio-a" };
            var plano = new PlanoAssinatura { Nome = "Padrão", ValorFixo = 100, ValorPorAluno = 2, ValorMinimo = 150 };
            _context.Tenants.Add(instituicao);
            _context.PlanosAssinatura.Add(plano);
            _context.Assinaturas.Add(new Assinatura
            {
                Tenant = instituicao, Plano = plano, DataInicio = new DateOnly(2026, 1, 1), DiaVencimento = 10,
                DescontoPercentual = 10, TesteAte = testeAte,
                Situacao = testeAte is null ? Assinatura.SituacaoAtiva : Assinatura.SituacaoTeste,
                RazaoSocial = "Colégio A Ltda", CpfCnpj = "11222333000181", Email = "financeiro@colegio-a.com",
            });

            // 30 ativos enturmados, 1 inativo e 1 com enturmação encerrada: contam 30.
            for (var i = 1; i <= 32; i++)
            {
                _context.Alunos.Add(new Aluno
                {
                    Id = i, TenantId = TenantId, Nome = $"Aluno {i}", Matricula = $"2026{i:D4}",
                    Status = i == 31 ? Aluno.StatusInativo : Aluno.StatusAtivo,
                });
                _context.AlunosTurmas.Add(new AlunoTurma
                {
                    TenantId = TenantId, AlunoId = i, TurmaId = 1, DataInicio = new DateOnly(2026, 2, 1),
                    DataFim = i == 32 ? new DateOnly(2026, 6, 30) : null,
                });
            }
            _context.SaveChanges();

            Asaas = new FakeAsaas(asaasConfigurado);
            var faturamento = new FaturamentoOptions
            {
                NotaFiscal = new NotaFiscalOptions { Habilitada = notaFiscal, MunicipalServiceName = "Licenciamento de software" },
            };
            Service = new FaturamentoService(new FaturamentoRepository(_context), Asaas, Options.Create(faturamento),
                NullLogger<FaturamentoService>.Instance);
        }

        public List<FaturaAssinatura> Faturas() => _context.FaturasAssinatura.ToList();
        public Assinatura AssinaturaAtual() => _context.Assinaturas.Single();
        public Tenant Instituicao() => _context.Tenants.Single();
    }

    private sealed class SemInstituicao : ITenantContext
    {
        public int? TenantId => null;
        public string? TenantSlug => null;
    }

    public sealed class FakeAsaas(bool configurado) : IAsaasClient
    {
        public List<AsaasNovaCobranca> Cobrancas { get; } = [];
        public List<string> Canceladas { get; } = [];
        public List<AsaasNovaNotaFiscal> NotasFiscais { get; } = [];
        public string? FalharCobranca { get; set; }

        public bool Configurado => configurado;

        public Task<string> SalvarClienteAsync(string? clienteId, AsaasCliente cliente, CancellationToken ct = default)
            => Task.FromResult(clienteId ?? "cus_1");

        public Task<AsaasCobranca> CriarCobrancaAsync(AsaasNovaCobranca cobranca, CancellationToken ct = default)
        {
            if (FalharCobranca is not null)
                throw new AsaasException(FalharCobranca);

            Cobrancas.Add(cobranca);
            return Task.FromResult(new AsaasCobranca($"pay_{Cobrancas.Count}", "PENDING", $"https://asaas/i/{Cobrancas.Count}"));
        }

        public Task CancelarCobrancaAsync(string cobrancaId, CancellationToken ct = default)
        {
            Canceladas.Add(cobrancaId);
            return Task.CompletedTask;
        }

        public Task ConfirmarRecebimentoEmDinheiroAsync(string cobrancaId, DateOnly data, decimal valor, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<AsaasNotaFiscal> AgendarNotaFiscalAsync(AsaasNovaNotaFiscal nota, CancellationToken ct = default)
        {
            NotasFiscais.Add(nota);
            return Task.FromResult(new AsaasNotaFiscal($"inv_{NotasFiscais.Count}", "SCHEDULED"));
        }
    }
}
