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
/// Fluxo HTTP da chamada: lançamento, alteração, histórico, frequência e o escopo do professor
/// (só turmas/disciplinas alocadas), com permissões reais e banco InMemory.
/// </summary>
public class ChamadasApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public ChamadasApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task Gestao_LancaAlteraEConsultaFrequencia()
    {
        // História: o professor do cenário não tem acesso, então os outros testes não interferem aqui.
        var client = ClientDoUsuario(_cenario.SecretarioId);
        var data = Hoje.AddDays(-1);

        var lista = await client.GetFromJsonAsync<JsonElement>(
            $"/api/chamadas/aula?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}&data={data:yyyy-MM-dd}");
        Assert.Equal(JsonValueKind.Null, lista.GetProperty("chamadaId").ValueKind);
        Assert.Equal(2, lista.GetProperty("alunos").GetArrayLength());

        var criacao = await client.PostAsJsonAsync("/api/chamadas", new
        {
            turmaId = _cenario.TurmaId,
            disciplinaId = _cenario.HistoriaId,
            data = data.ToString("yyyy-MM-dd"),
            quantidadeAulas = 2,
            conteudo = "Frações",
            alunos = new object[]
            {
                new { alunoId = _cenario.AnaId, situacao = "PRESENTE" },
                new { alunoId = _cenario.BrunoId, situacao = "FALTA" },
            },
        });
        Assert.Equal(HttpStatusCode.OK, criacao.StatusCode);
        var criada = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var chamadaId = criada.GetProperty("chamadaId").GetInt32();
        Assert.Equal("sec@escola-b.com", criada.GetProperty("registradoPor").GetString());

        var duplicada = await client.PostAsJsonAsync("/api/chamadas", new
        {
            turmaId = _cenario.TurmaId, disciplinaId = _cenario.HistoriaId, data = data.ToString("yyyy-MM-dd"),
            quantidadeAulas = 1, alunos = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);

        var alteracao = await client.PutAsJsonAsync($"/api/chamadas/{chamadaId}", new
        {
            quantidadeAulas = 2,
            conteudo = "Frações equivalentes",
            alunos = new object[]
            {
                new { alunoId = _cenario.AnaId, situacao = "PRESENTE" },
                new { alunoId = _cenario.BrunoId, situacao = "FALTA_JUSTIFICADA", justificativa = "Atestado" },
            },
        });
        Assert.Equal(HttpStatusCode.OK, alteracao.StatusCode);

        var historico = await client.GetFromJsonAsync<JsonElement>(
            $"/api/chamadas?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}");
        var item = historico.EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == chamadaId);
        Assert.Equal((1, 0, 1), (item.GetProperty("presentes").GetInt32(), item.GetProperty("faltas").GetInt32(),
            item.GetProperty("faltasJustificadas").GetInt32()));

        var frequencia = await client.GetFromJsonAsync<JsonElement>(
            $"/api/chamadas/frequencia?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}");
        var bruno = frequencia.GetProperty("alunos").EnumerateArray().Single(a => a.GetProperty("alunoId").GetInt32() == _cenario.BrunoId);
        Assert.Equal(2, bruno.GetProperty("faltasJustificadas").GetInt32());
        Assert.Equal(0m, bruno.GetProperty("percentualFrequencia").GetDecimal());
        Assert.True(bruno.GetProperty("abaixoDoMinimo").GetBoolean());
    }

    [Fact]
    public async Task Professor_SoAcessaTurmasEDisciplinasAlocadas()
    {
        var client = ClientDoUsuario(_cenario.ProfessorUsuarioId);

        var turmas = await client.GetFromJsonAsync<JsonElement>("/api/chamadas/turmas");
        var turma = Assert.Single(turmas.EnumerateArray());
        Assert.Equal(["Matemática"], turma.GetProperty("disciplinas").EnumerateArray().Select(d => d.GetProperty("nome").GetString()));

        var naoAlocada = await client.PostAsJsonAsync("/api/chamadas", Corpo(_cenario.HistoriaId, Hoje));
        var alocada = await client.PostAsJsonAsync("/api/chamadas", Corpo(_cenario.MatematicaId, Hoje));

        Assert.Equal(HttpStatusCode.Forbidden, naoAlocada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alocada.StatusCode);
    }

    [Fact]
    public async Task UsuarioSemPermissaoDeChamada_Recebe403()
    {
        var client = ClientDoUsuario(_factory.UsuarioBId);

        var response = await client.GetAsync("/api/chamadas/turmas");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private object Corpo(int disciplinaId, DateOnly data) => new
    {
        turmaId = _cenario.TurmaId,
        disciplinaId,
        data = data.ToString("yyyy-MM-dd"),
        quantidadeAulas = 1,
        alunos = new object[]
        {
            new { alunoId = _cenario.AnaId, situacao = "PRESENTE" },
            new { alunoId = _cenario.BrunoId, situacao = "PRESENTE" },
        },
    };

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
    /// Escola B: turma "6º Ano A" com Ana e Bruno; Matemática e História na grade. Um secretário
    /// (gestão) e um professor alocado só em Matemática, ambos com as permissões de chamada.
    /// </summary>
    private sealed record Cenario(
        int TurmaId, int MatematicaId, int HistoriaId, int AnaId, int BrunoId, int SecretarioId, int ProfessorUsuarioId)
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

                var escola = new Escola { TenantId = tenantId, Nome = "Escola B1", CodigoInep = "1" };
                var etapa = new EtapaEnsino { TenantId = tenantId, ModalidadeEnsinoId = factory.ModalidadeBId, Nome = "6º Ano", Sigla = "6A" };
                var ano = new AnoLetivo
                {
                    TenantId = tenantId, AnoReferencia = Hoje.Year, TipoPeriodo = AnoLetivo.TipoBimestral,
                    DataInicio = Hoje.AddDays(-30), DataTermino = Hoje.AddDays(30),
                };
                db.AddRange(escola, etapa, ano);
                db.SaveChanges();

                var turma = new Turma
                {
                    TenantId = tenantId, AnoLetivoId = ano.Id, EscolaId = escola.Id, ModalidadeEnsinoId = factory.ModalidadeBId,
                    EtapaEnsinoId = etapa.Id, NomeIdentificador = "A", NomeCompleto = "6º Ano A", VagasOfertadas = 30,
                };
                var matematica = new Disciplina { TenantId = tenantId, Nome = "Matemática", Codigo = "MAT" };
                var historia = new Disciplina { TenantId = tenantId, Nome = "História", Codigo = "HIS" };
                var ana = new Aluno { TenantId = tenantId, Nome = "Ana Souza", Matricula = "1", EscolaId = escola.Id, Status = Aluno.StatusAtivo };
                var bruno = new Aluno { TenantId = tenantId, Nome = "Bruno Lima", Matricula = "2", EscolaId = escola.Id, Status = Aluno.StatusAtivo };
                var secretario = new User { TenantId = tenantId, Email = "sec@escola-b.com", Cpf = "39053344705" };
                var usuarioProfessor = new User { TenantId = tenantId, Email = "prof@escola-b.com", Cpf = "86288366757" };
                var perfilSecretario = new Perfil { Nome = Perfil.Secretario };
                var perfilProfessor = new Perfil { Nome = Perfil.Professor };
                db.AddRange(turma, matematica, historia, ana, bruno, secretario, usuarioProfessor, perfilSecretario, perfilProfessor);
                db.SaveChanges();

                var professor = new Professor { TenantId = tenantId, Nome = "Prof. Carlos", UsuarioId = usuarioProfessor.Id, Cpf = "86288366757" };
                // O professor só acessa as escolas em que leciona.
                professor.ProfessorEscolas.Add(new ProfessorEscola { TenantId = tenantId, EscolaId = escola.Id });
                db.Add(professor);
                db.AddRange(
                    new DisciplinaEtapaEnsino { TenantId = tenantId, DisciplinaId = matematica.Id, EtapaEnsinoId = etapa.Id },
                    new DisciplinaEtapaEnsino { TenantId = tenantId, DisciplinaId = historia.Id, EtapaEnsinoId = etapa.Id },
                    new AlunoTurma { TenantId = tenantId, AlunoId = ana.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new AlunoTurma { TenantId = tenantId, AlunoId = bruno.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new UsuarioPerfil { UsuarioId = secretario.Id, PerfilId = perfilSecretario.Id },
                    new UsuarioPerfil { UsuarioId = usuarioProfessor.Id, PerfilId = perfilProfessor.Id });
                db.AddRange(new[] { perfilSecretario, perfilProfessor }.SelectMany(perfil =>
                    new[] { Permissoes.Chamada.Visualizar, Permissoes.Chamada.Criar, Permissoes.Chamada.Editar }
                        .Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfil.Id, Permissao = p })));
                db.SaveChanges();

                db.Add(new ProfessorAlocacao { TenantId = tenantId, ProfessorId = professor.Id, TurmaId = turma.Id, DisciplinaId = matematica.Id });
                db.SaveChanges();

                var cenario = new Cenario(turma.Id, matematica.Id, historia.Id, ana.Id, bruno.Id, secretario.Id, usuarioProfessor.Id);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }
    }
}
