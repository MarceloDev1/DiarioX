using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Tests.API;

/// <summary>
/// Testes de ponta a ponta das permissões por perfil: [Permissao] nos controllers,
/// herança pelos perfis do usuário e a API de Configurações.
/// </summary>
public class PermissoesApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public PermissoesApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task UsuarioSemPerfil_LeCadastrosBasicosMasNaoAltera()
    {
        var client = ClientDoUsuario(_factory.UsuarioBId);

        var leitura = await client.GetAsync("/api/modalidadesensino");
        var criacao = await client.PostAsJsonAsync("/api/modalidadesensino", new { nome = "Nova", sigla = "NV" });

        Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, criacao.StatusCode);
    }

    [Fact]
    public async Task UsuarioComPermissao_PassaPelaAutorizacao()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);

        var criacao = await client.PostAsJsonAsync("/api/modalidadesensino", new { nome = "", sigla = "" });

        // Passou da autorização; a validação do serviço decide o resultado.
        Assert.NotEqual(HttpStatusCode.Forbidden, criacao.StatusCode);
    }

    [Fact]
    public async Task UsuarioSemPermissaoDeVisualizar_NaoAcessaDadosPessoais()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);

        var usuarios = await client.GetAsync("/api/users");
        var permissoes = await client.GetAsync("/api/permissoes/perfis");

        Assert.Equal(HttpStatusCode.Forbidden, usuarios.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, permissoes.StatusCode);
    }

    [Fact]
    public async Task Minhas_RetornaUniaoDasPermissoesDosPerfisDoUsuario()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);

        var permissoes = await client.GetFromJsonAsync<List<string>>("/api/permissoes/minhas");

        Assert.Equal(["alunos.visualizar", "modalidades-ensino.criar", "modalidades-ensino.visualizar"], permissoes);
    }

    [Fact]
    public async Task Gerencia_AlteraPermissoesDeOutroPerfil_ComEfeitoImediato()
    {
        var gerente = ClientDoUsuario(_cenario.GerenteId);
        var financeiro = ClientDoUsuario(_cenario.FinanceiroId);

        var antes = await financeiro.GetFromJsonAsync<List<string>>("/api/permissoes/minhas");
        var atualizacao = await gerente.PutAsJsonAsync(
            $"/api/permissoes/perfis/{_cenario.PerfilFinanceiroId}",
            new { permissoes = new[] { "usuarios.criar" } });
        var depois = await financeiro.GetFromJsonAsync<List<string>>("/api/permissoes/minhas");

        Assert.Empty(antes!);
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        // Criar implica visualizar o módulo.
        Assert.Equal(["usuarios.criar", "usuarios.visualizar"], depois);
    }

    [Fact]
    public async Task Gerencia_NaoPodeRemoverPropriaPermissaoDeConfiguracoes()
    {
        var gerente = ClientDoUsuario(_cenario.GerenteId);

        var response = await gerente.PutAsJsonAsync(
            $"/api/permissoes/perfis/{_cenario.PerfilGerenciaId}",
            new { permissoes = new[] { "alunos.visualizar" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PermissaoInexistente_RetornaBadRequest()
    {
        var gerente = ClientDoUsuario(_cenario.GerenteId);

        var response = await gerente.PutAsJsonAsync(
            $"/api/permissoes/perfis/{_cenario.PerfilFinanceiroId}",
            new { permissoes = new[] { "alunos.aprovar" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdministradorGlobal_NaInstituicao_TemTodasAsPermissoes()
    {
        var client = CreateClient(HostEscolaB,
            CreateToken(_factory.GlobalAdminId, "admin@diariox.local", _factory.TenantB, globalAdmin: true));

        var permissoes = await client.GetFromJsonAsync<List<string>>("/api/permissoes/minhas");
        var criacao = await client.PostAsJsonAsync("/api/modalidadesensino", new { nome = "", sigla = "" });

        Assert.Equal(Permissoes.Todas.Count, permissoes!.Count);
        Assert.NotEqual(HttpStatusCode.Forbidden, criacao.StatusCode);
    }

    private HttpClient ClientDoUsuario(int userId)
        => CreateClient(HostEscolaB, CreateToken(userId, $"usuario{userId}@escola-b.com", _factory.TenantB, globalAdmin: false));

    private HttpClient CreateClient(string baseAddress, string token)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress),
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string CreateToken(int userId, string email, Tenant tenant, bool globalAdmin)
    {
        var jwt = _factory.Services.GetRequiredService<IConfiguration>().GetSection("Jwt");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaimTypes.TenantId, tenant.Id.ToString()),
            new(AppClaimTypes.TenantSlug, tenant.Slug),
        };

        if (globalAdmin)
            claims.Add(new Claim(AppClaimTypes.GlobalAdmin, "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Perfis e usuários da Escola B com permissões conhecidas, criados uma vez por fixture.</summary>
    private sealed record Cenario(
        int PerfilGerenciaId, int PerfilFinanceiroId, int GerenteId, int SecretarioId, int FinanceiroId)
    {
        private static readonly Dictionary<TenancyApiFactory, Cenario> PorFactory = new();

        public static Cenario Obter(TenancyApiFactory factory)
        {
            lock (PorFactory)
            {
                if (PorFactory.TryGetValue(factory, out var existente))
                    return existente;

                using var scope = factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tenantId = factory.TenantB.Id;

                var gerencia = new Perfil { Nome = Perfil.Gerencia };
                var secretario = new Perfil { Nome = Perfil.Secretario };
                var financeiro = new Perfil { Nome = Perfil.Financeiro };
                db.Perfis.AddRange(gerencia, secretario, financeiro);

                var gerente = new User { TenantId = tenantId, Email = "gerente@escola-b.com", Cpf = "52998224725" };
                var usuarioSecretario = new User { TenantId = tenantId, Email = "sec@escola-b.com", Cpf = "39053344705" };
                var usuarioFinanceiro = new User { TenantId = tenantId, Email = "fin@escola-b.com", Cpf = "86288366757" };
                db.Users.AddRange(gerente, usuarioSecretario, usuarioFinanceiro);
                db.SaveChanges();

                db.UsuariosPerfis.AddRange(
                    new UsuarioPerfil { UsuarioId = gerente.Id, PerfilId = gerencia.Id },
                    new UsuarioPerfil { UsuarioId = usuarioSecretario.Id, PerfilId = secretario.Id },
                    new UsuarioPerfil { UsuarioId = usuarioFinanceiro.Id, PerfilId = financeiro.Id });

                db.PerfisPermissoes.AddRange(
                    Permissoes.Todas.Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = gerencia.Id, Permissao = p }));
                db.PerfisPermissoes.AddRange(
                    new PerfilPermissao { TenantId = tenantId, PerfilId = secretario.Id, Permissao = "alunos.visualizar" },
                    new PerfilPermissao { TenantId = tenantId, PerfilId = secretario.Id, Permissao = "modalidades-ensino.visualizar" },
                    new PerfilPermissao { TenantId = tenantId, PerfilId = secretario.Id, Permissao = "modalidades-ensino.criar" });
                db.SaveChanges();

                var cenario = new Cenario(gerencia.Id, financeiro.Id, gerente.Id, usuarioSecretario.Id, usuarioFinanceiro.Id);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }
    }
}
