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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Tests.API;

/// <summary>
/// Relatórios pela API real: permissões, escopo por escola (filtros globais + resolução do escopo no
/// middleware) e exportação em Excel e PDF.
/// </summary>
public class RelatoriosApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public RelatoriosApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task UsuarioSemPermissaoDeRelatorios_Recebe403()
    {
        var response = await ClientDoUsuario(_factory.UsuarioBId).GetAsync("/api/relatorios");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Catalogo_MostraSoRelatoriosCujosDadosOPerfilPodeVer()
    {
        var diretor = await ClientDoUsuario(_cenario.DiretorId).GetFromJsonAsync<JsonElement>("/api/relatorios");
        var gerencia = await ClientDoUsuario(_cenario.GerenciaId).GetFromJsonAsync<JsonElement>("/api/relatorios");

        Assert.Equal(["ocupacao-vagas"], Ids(diretor));
        Assert.Equal(["alunos-aguardando-enturmacao", "ocupacao-vagas", "relacao-alunos-turma"], Ids(gerencia).Order());
    }

    [Fact]
    public async Task Ocupacao_RespeitaAsEscolasDoUsuario()
    {
        var diretor = await ClientDoUsuario(_cenario.DiretorId).GetFromJsonAsync<JsonElement>("/api/relatorios/ocupacao-vagas");
        var gerencia = await ClientDoUsuario(_cenario.GerenciaId).GetFromJsonAsync<JsonElement>("/api/relatorios/ocupacao-vagas");

        Assert.Equal(["Escola Norte"], Escolas(diretor));
        Assert.Equal(["Escola Norte", "Escola Sul"], Escolas(gerencia));

        // Escola Norte: 2 vagas, 1 aluno ativo enturmado (o inativo não conta).
        var norte = diretor.GetProperty("linhas")[0];
        Assert.Equal(2, norte[3].GetInt32());
        Assert.Equal(1, norte[4].GetInt32());
        Assert.Equal(50m, norte[6].GetDecimal());
    }

    [Fact]
    public async Task Ocupacao_FiltroDeEscolaForaDoEscopo_RetornaNaoEncontrada()
    {
        var response = await ClientDoUsuario(_cenario.DiretorId)
            .GetAsync($"/api/relatorios/ocupacao-vagas?escolaId={_cenario.EscolaSulId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RelatorioSemPermissaoDoModulo_Recebe403()
    {
        var response = await ClientDoUsuario(_cenario.DiretorId)
            .GetAsync($"/api/relatorios/relacao-alunos-turma?turmaId={_cenario.TurmaNorteId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PK")]
    [InlineData("pdf", "application/pdf", "%PDF")]
    public async Task Exportacao_GeraArquivoNoFormatoPedido(string formato, string contentType, string assinatura)
    {
        var response = await ClientDoUsuario(_cenario.GerenciaId)
            .GetAsync($"/api/relatorios/relacao-alunos-turma?turmaId={_cenario.TurmaNorteId}&formato={formato}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.EndsWith($".{formato}", response.Content.Headers.ContentDisposition?.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(assinatura, Encoding.ASCII.GetString(bytes, 0, assinatura.Length));
    }

    [Fact]
    public async Task FormatoInvalido_Retorna400()
    {
        var response = await ClientDoUsuario(_cenario.GerenciaId).GetAsync("/api/relatorios/ocupacao-vagas?formato=doc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static IEnumerable<string> Ids(JsonElement catalogo)
        => catalogo.EnumerateArray().Select(r => r.GetProperty("id").GetString()!);

    private static IEnumerable<string> Escolas(JsonElement relatorio)
        => relatorio.GetProperty("linhas").EnumerateArray().Select(l => l[0].GetString()!).Distinct().Order();

    private HttpClient ClientDoUsuario(int userId)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(HostEscolaB),
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(userId));
        return client;
    }

    private string CreateToken(int userId)
    {
        var jwt = _factory.Services.GetRequiredService<IConfiguration>().GetSection("Jwt");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaimTypes.TenantId, _factory.TenantB.Id.ToString()),
            new(AppClaimTypes.TenantSlug, _factory.TenantB.Slug),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Duas escolas com uma turma de 2 vagas cada. Na Norte: um aluno ativo e um inativo enturmados.
    /// Diretor restrito à Norte (relatórios + turmas) e gerência da rede toda (relatórios + turmas + alunos).
    /// </summary>
    private sealed record Cenario(int EscolaSulId, int TurmaNorteId, int DiretorId, int GerenciaId)
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
                var hoje = DateOnly.FromDateTime(DateTime.Today);

                var norte = new Escola { TenantId = tenantId, Nome = "Escola Norte", CodigoInep = "10" };
                var sul = new Escola { TenantId = tenantId, Nome = "Escola Sul", CodigoInep = "20" };
                var etapa = new EtapaEnsino { TenantId = tenantId, ModalidadeEnsinoId = factory.ModalidadeBId, Nome = "1º Ano", Sigla = "1A" };
                var ano = new AnoLetivo
                {
                    TenantId = tenantId, AnoReferencia = hoje.Year, TipoPeriodo = AnoLetivo.TipoBimestral,
                    DataInicio = hoje.AddDays(-60), DataTermino = hoje.AddDays(60),
                };
                var diretor = new User { TenantId = tenantId, Email = "diretor@norte.com", Cpf = "39053344705" };
                var gerencia = new User { TenantId = tenantId, Email = "gerencia@rede.com", Cpf = "86288366757" };
                var perfilDiretor = new Perfil { Nome = Perfil.Diretor };
                var perfilGerencia = new Perfil { Nome = Perfil.Gerencia };
                db.AddRange(norte, sul, etapa, ano, diretor, gerencia, perfilDiretor, perfilGerencia);
                db.SaveChanges();

                var turmas = new[] { norte, sul }.Select(escola => new Turma
                {
                    TenantId = tenantId, AnoLetivoId = ano.Id, EscolaId = escola.Id, ModalidadeEnsinoId = factory.ModalidadeBId,
                    EtapaEnsinoId = etapa.Id, NomeIdentificador = "A", NomeCompleto = $"1º Ano A - {escola.Nome}", VagasOfertadas = 2,
                }).ToArray();
                var ativo = NovoAluno(tenantId, norte.Id, "Ana", "1", Aluno.StatusAtivo);
                var inativo = NovoAluno(tenantId, norte.Id, "Bia", "2", Aluno.StatusInativo);
                db.AddRange(turmas);
                db.AddRange(ativo, inativo);
                db.SaveChanges();

                db.AddRange(
                    new AlunoTurma { TenantId = tenantId, AlunoId = ativo.Id, TurmaId = turmas[0].Id, DataInicio = ano.DataInicio },
                    new AlunoTurma { TenantId = tenantId, AlunoId = inativo.Id, TurmaId = turmas[0].Id, DataInicio = ano.DataInicio },
                    new UsuarioPerfil { UsuarioId = diretor.Id, PerfilId = perfilDiretor.Id, EscolaId = norte.Id },
                    new UsuarioPerfil { UsuarioId = gerencia.Id, PerfilId = perfilGerencia.Id });
                db.AddRange(
                    new[] { Permissoes.Relatorios.Visualizar, Permissoes.Turmas.Visualizar }
                        .Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilDiretor.Id, Permissao = p })
                        .Concat(new[] { Permissoes.Relatorios.Visualizar, Permissoes.Turmas.Visualizar, Permissoes.Alunos.Visualizar }
                            .Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilGerencia.Id, Permissao = p })));
                db.SaveChanges();

                var cenario = new Cenario(sul.Id, turmas[0].Id, diretor.Id, gerencia.Id);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }

        private static Aluno NovoAluno(int tenantId, int escolaId, string nome, string matricula, string status) => new()
        {
            TenantId = tenantId, EscolaId = escolaId, Nome = nome, Matricula = matricula, Status = status,
            Sexo = Aluno.SexoFeminino, DataNascimento = new DateTime(2019, 3, 10),
            ResponsavelNome1 = "Responsável", ResponsavelTelefone1 = "11987654321",
        };
    }
}
