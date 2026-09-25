using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Auth;
using DiarioX.Server.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Tests.API;

/// <summary>
/// Testes de ponta a ponta do pipeline HTTP multitenant: resolução por subdomínio,
/// políticas de autorização, seleção de instituição e isolamento dos dados.
/// </summary>
public class TenancyApiTests : IClassFixture<TenancyApiFactory>
{
    private const string AdminHost = "http://localhost";

    private readonly TenancyApiFactory _factory;

    public TenancyApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    [Fact]
    public async Task RotaDaApi_SemToken_Retorna401()
    {
        var client = CreateClient(AdminHost);

        var response = await client.GetAsync("/api/modalidadesensino");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TenantAtual_NoHostDeAdministracao_Retorna204()
    {
        var response = await CreateClient(AdminHost).GetAsync("/api/tenants/current");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task TenantAtual_NoSubdominioDaInstituicao_RetornaNome()
    {
        var response = await CreateClient("http://escola-a.dev.localhost").GetAsync("/api/tenants/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("Escola A", body!["nome"]);
    }

    [Fact]
    public async Task SubdominioDesconhecido_Retorna404()
    {
        var response = await CreateClient("http://nao-existe.dev.localhost").PostAsJsonAsync(
            "/api/auth/login", new { login = "usuario@x.com", password = "Senha@123" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubdominioDeInstituicaoInativa_Retorna403()
    {
        var response = await CreateClient("http://escola-inativa.dev.localhost").GetAsync("/api/tenants/current");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministradorGlobal_SemInstituicao_NaoAcessaDadosMasListaInstituicoes()
    {
        var client = CreateClient(AdminHost, CreateToken(_factory.GlobalAdminId, "admin@diariox.local", tenant: null, globalAdmin: true));

        var dados = await client.GetAsync("/api/modalidadesensino");
        var instituicoes = await client.GetAsync("/api/tenants");

        Assert.Equal(HttpStatusCode.Forbidden, dados.StatusCode);
        Assert.Equal(HttpStatusCode.OK, instituicoes.StatusCode);
        var nomes = (await instituicoes.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>())!
            .Select(t => t["nome"].ToString())
            .ToList();
        Assert.Contains("Escola A", nomes);
        Assert.Contains("Escola B", nomes);
    }

    [Fact]
    public async Task AdministradorGlobal_AoSelecionarInstituicao_VeApenasDadosDela()
    {
        var adminClient = CreateClient(AdminHost, CreateToken(_factory.GlobalAdminId, "admin@diariox.local", tenant: null, globalAdmin: true));

        var selecao = await adminClient.PostAsJsonAsync("/api/auth/select-tenant", new { tenantId = _factory.TenantA.Id });
        Assert.Equal(HttpStatusCode.OK, selecao.StatusCode);
        var login = (await selecao.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.Equal(_factory.TenantA.Id, login.TenantId);
        Assert.True(login.IsGlobalAdmin);

        var clientNaEscolaA = CreateClient(AdminHost, login.Token);
        var modalidades = await clientNaEscolaA.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/modalidadesensino");

        Assert.Equal(["Fundamental A"], modalidades!.Select(m => m["nome"].ToString()).ToList());
    }

    [Fact]
    public async Task AdministradorGlobal_NaoSelecionaInstituicaoInativa()
    {
        var client = CreateClient(AdminHost, CreateToken(_factory.GlobalAdminId, "admin@diariox.local", tenant: null, globalAdmin: true));

        var response = await client.PostAsJsonAsync("/api/auth/select-tenant", new { tenantId = _factory.TenantInativo.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UsuarioDaInstituicao_VeApenasSeusDados()
    {
        var client = CreateClient(
            "http://escola-b.dev.localhost",
            CreateToken(_factory.UsuarioBId, "secretaria@escola-b.com", _factory.TenantB, globalAdmin: false));

        var modalidades = await client.GetFromJsonAsync<List<Dictionary<string, object>>>("/api/modalidadesensino");
        var modalidadeDaEscolaA = await client.GetAsync($"/api/modalidadesensino/{_factory.ModalidadeAId}");

        Assert.Equal(["Fundamental B"], modalidades!.Select(m => m["nome"].ToString()).ToList());
        Assert.Equal(HttpStatusCode.NotFound, modalidadeDaEscolaA.StatusCode);
    }

    [Fact]
    public async Task UsuarioDaInstituicao_NaoAcessaRotasDoAdministradorGlobal()
    {
        var client = CreateClient(
            "http://escola-b.dev.localhost",
            CreateToken(_factory.UsuarioBId, "secretaria@escola-b.com", _factory.TenantB, globalAdmin: false));

        var instituicoes = await client.GetAsync("/api/tenants");
        var selecao = await client.PostAsJsonAsync("/api/auth/select-tenant", new { tenantId = _factory.TenantA.Id });

        Assert.Equal(HttpStatusCode.Forbidden, instituicoes.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, selecao.StatusCode);
    }

    [Fact]
    public async Task Swagger_ContinuaAcessivelEComEsquemaBearer()
    {
        var response = await CreateClient(AdminHost).GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("bearer", await response.Content.ReadAsStringAsync());
    }

    private HttpClient CreateClient(string baseAddress, string? token = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress),
            AllowAutoRedirect = false,
        });

        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    // Emite um token com as mesmas claims do AuthService (o login por e-mail/CPF usa ILike,
    // que o provider InMemory não suporta).
    private string CreateToken(int userId, string email, Tenant? tenant, bool globalAdmin)
    {
        var jwt = _factory.Services.GetRequiredService<IConfiguration>().GetSection("Jwt");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
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
