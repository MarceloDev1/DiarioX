using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Tests.API;

/// <summary>
/// Pipeline HTTP do faturamento: webhook autenticado por token, bloqueio de gravação da
/// instituição em somente leitura e rotas exclusivas do Administrador global.
/// </summary>
public class FaturamentoApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";
    private const string AdminHost = "http://localhost";

    private readonly TenancyApiFactory _factory;
    private readonly int _gestorId;

    public FaturamentoApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _gestorId = SemearGestorEmSomenteLeitura(factory);
    }

    [Fact]
    public async Task Webhook_SemTokenValido_Retorna401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(AdminHost) });

        var semToken = await client.PostAsJsonAsync("/api/webhooks/asaas", new { @event = "PAYMENT_RECEIVED" });

        client.DefaultRequestHeaders.Add("asaas-access-token", "token-errado");
        var tokenErrado = await client.PostAsJsonAsync("/api/webhooks/asaas", new { @event = "PAYMENT_RECEIVED" });

        Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, tokenErrado.StatusCode);
    }

    [Fact]
    public async Task Webhook_ComToken_EventoSemFaturaRetorna200()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri(AdminHost) });
        client.DefaultRequestHeaders.Add("asaas-access-token", TenancyApiFactory.TokenWebhookAsaas);

        var response = await client.PostAsJsonAsync("/api/webhooks/asaas", new
        {
            id = "evt_1",
            @event = "PAYMENT_RECEIVED",
            payment = new { id = "pay_inexistente", status = "RECEIVED", value = 10.5, billingType = "PIX", paymentDate = "2026-10-05" },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SomenteLeitura_BloqueiaGravacaoMasLiberaConsultaEChamada()
    {
        var client = Client(HostEscolaB, CreateToken(_gestorId, _factory.TenantB, globalAdmin: false));

        var consulta = await client.GetAsync("/api/modalidadesensino");
        var gravacao = await client.PostAsJsonAsync("/api/modalidadesensino", new { nome = "Nova", sigla = "NV" });
        var chamada = await client.PostAsJsonAsync("/api/chamadas", new { turmaId = 999, disciplinaId = 999 });
        var aviso = await client.GetFromJsonAsync<JsonElement>("/api/assinatura/aviso");

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, gravacao.StatusCode);
        Assert.Equal("SOMENTE_LEITURA", (await gravacao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
        // A chamada passa pelo bloqueio e chega ao serviço (turma inexistente → 404).
        Assert.Equal(HttpStatusCode.NotFound, chamada.StatusCode);
        Assert.Equal("SOMENTE_LEITURA", aviso.GetProperty("situacaoFinanceira").GetString());
    }

    [Fact]
    public async Task SomenteLeitura_NaoBloqueiaAdministradorGlobal()
    {
        var client = Client(HostEscolaB, CreateToken(_factory.GlobalAdminId, _factory.TenantB, globalAdmin: true));

        var gravacao = await client.PostAsJsonAsync("/api/modalidadesensino", new { nome = "", sigla = "" });

        Assert.NotEqual(HttpStatusCode.Forbidden, gravacao.StatusCode);
    }

    [Fact]
    public async Task Faturamento_ExclusivoDoAdministradorGlobal()
    {
        var usuario = Client(HostEscolaB, CreateToken(_gestorId, _factory.TenantB, globalAdmin: false));
        var admin = Client(AdminHost, CreateToken(_factory.GlobalAdminId, tenant: null, globalAdmin: true));

        var negado = await usuario.GetAsync("/api/faturamento/painel");
        var painel = await admin.GetAsync("/api/faturamento/painel");
        var plano = await admin.PostAsJsonAsync("/api/faturamento/planos",
            new { nome = "Por aluno", valorFixo = 0, valorPorAluno = 3.5, valorMinimo = 99, ativo = true });

        Assert.Equal(HttpStatusCode.Forbidden, negado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, painel.StatusCode);
        Assert.Equal(HttpStatusCode.OK, plano.StatusCode);
    }

    /// <summary>Escola B em somente leitura e um gestor com permissões de cadastro e chamada.</summary>
    private static int SemearGestorEmSomenteLeitura(TenancyApiFactory factory)
    {
        lock (factory)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var existente = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Email == "gestor@escola-b.com");
            if (existente is not null)
                return existente.Id;

            var tenantId = factory.TenantB.Id;
            db.Tenants.Single(t => t.Id == tenantId).SituacaoFinanceira = Tenant.SituacaoFinanceiraSomenteLeitura;

            var perfil = new Perfil { Nome = Perfil.Gerencia };
            var gestor = new User { TenantId = tenantId, Email = "gestor@escola-b.com", Cpf = "52998224725" };
            db.AddRange(perfil, gestor);
            db.SaveChanges();

            db.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = gestor.Id, PerfilId = perfil.Id });
            db.PerfisPermissoes.AddRange(Permissoes.Todas.Select(p =>
                new PerfilPermissao { TenantId = tenantId, PerfilId = perfil.Id, Permissao = p }));
            db.SaveChanges();
            return gestor.Id;
        }
    }

    private HttpClient Client(string baseAddress, string token)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress),
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string CreateToken(int userId, Tenant? tenant, bool globalAdmin)
    {
        var jwt = _factory.Services.GetRequiredService<IConfiguration>().GetSection("Jwt");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        if (globalAdmin)
            claims.Add(new Claim(AppClaimTypes.GlobalAdmin, "true"));
        if (tenant is not null)
        {
            claims.Add(new Claim(AppClaimTypes.TenantId, tenant.Id.ToString()));
            claims.Add(new Claim(AppClaimTypes.TenantSlug, tenant.Slug));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
