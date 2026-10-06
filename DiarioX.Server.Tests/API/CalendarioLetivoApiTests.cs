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
/// RF005A pela API: cadastro de eventos, publicação, trava do diário de classe (RN01/EX01), limite do ano
/// letivo (EX02) e o escopo de escola na edição do calendário da rede. Banco próprio (uma factory por classe):
/// o calendário publicado aqui trava datas e não pode afetar os testes de chamada.
/// </summary>
public class CalendarioLetivoApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public CalendarioLetivoApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task EventoSemAulaPublicado_BloqueiaChamadaNaData()
    {
        var client = ClientDoUsuario(_cenario.GestorId);
        var dia = UltimoDiaUtilAntesDeHoje();

        var opcoes = await client.GetFromJsonAsync<JsonElement>("/api/calendarioletivo/opcoes");
        Assert.True(opcoes.GetProperty("podeEditarRede").GetBoolean());
        Assert.Contains(opcoes.GetProperty("anosLetivos").EnumerateArray(), a => a.GetProperty("id").GetInt32() == _cenario.AnoLetivoId);

        var salvo = await client.PostAsJsonAsync("/api/calendarioletivo/eventos", new
        {
            anoLetivoId = _cenario.AnoLetivoId,
            dataInicio = dia.ToString("yyyy-MM-dd"),
            tipo = EventoCalendario.TipoConselhoClasse,
            descricao = "I Conselho de Classe",
            comAula = false,
        });
        Assert.Equal(HttpStatusCode.OK, salvo.StatusCode);
        var calendario = (await salvo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("calendario");
        Assert.False(calendario.GetProperty("publicado").GetBoolean());
        Assert.Single(calendario.GetProperty("eventos").EnumerateArray());

        // Rascunho ainda não trava o diário.
        var antes = await GetAula(client, dia);
        Assert.Equal(JsonValueKind.Null, antes.GetProperty("bloqueio").ValueKind);

        var publicacao = await client.PostAsJsonAsync("/api/calendarioletivo/publicar", new { anoLetivoId = _cenario.AnoLetivoId });
        Assert.Equal(HttpStatusCode.OK, publicacao.StatusCode);
        var publicado = await publicacao.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"Calendário Letivo do Ano {Hoje.Year} configurado e publicado com sucesso!", publicado.GetProperty("message").GetString());

        const string mensagem =
            "Não é possível registrar frequência. Data configurada como I Conselho de Classe no Calendário Escolar.";
        var depois = await GetAula(client, dia);
        Assert.Equal(mensagem, depois.GetProperty("bloqueio").GetString());

        var chamada = await client.PostAsJsonAsync("/api/chamadas", new
        {
            turmaId = _cenario.TurmaId,
            disciplinaId = _cenario.DisciplinaId,
            data = dia.ToString("yyyy-MM-dd"),
            quantidadeAulas = 1,
            alunos = new[] { new { alunoId = _cenario.AlunoId, situacao = "PRESENTE" } },
        });
        Assert.Equal(HttpStatusCode.BadRequest, chamada.StatusCode);
        Assert.Equal(mensagem, (await chamada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task EventoForaDoAnoLetivo_RetornaEx02()
    {
        var client = ClientDoUsuario(_cenario.GestorId);

        var response = await client.PostAsJsonAsync("/api/calendarioletivo/eventos", new
        {
            anoLetivoId = _cenario.AnoLetivoId,
            dataInicio = _cenario.AnoLetivoInicio.AddDays(-1).ToString("yyyy-MM-dd"),
            tipo = EventoCalendario.TipoFeriado,
            descricao = "Confraternização Universal",
            comAula = false,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A data selecionada está fora do período do Ano Letivo configurado.",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task UsuarioDaEscola_AlteraSoOCalendarioDaEscola()
    {
        var client = ClientDoUsuario(_cenario.CoordenadorEscolaId);
        object Corpo(int? escolaId) => new
        {
            anoLetivoId = _cenario.AnoLetivoId,
            escolaId,
            dataInicio = _cenario.AnoLetivoInicio.ToString("yyyy-MM-dd"),
            tipo = EventoCalendario.TipoPlantaoPedagogico,
            descricao = "Plantão pedagógico",
            comAula = true,
        };

        var naRede = await client.PostAsJsonAsync("/api/calendarioletivo/eventos", Corpo(null));
        var naEscola = await client.PostAsJsonAsync("/api/calendarioletivo/eventos", Corpo(_cenario.EscolaId));

        Assert.Equal(HttpStatusCode.Forbidden, naRede.StatusCode);
        Assert.Equal(HttpStatusCode.OK, naEscola.StatusCode);
        var calendario = (await naEscola.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("calendario");
        Assert.Equal(_cenario.EscolaId, calendario.GetProperty("escolaId").GetInt32());
    }

    [Fact]
    public async Task UsuarioSemPermissao_Recebe403()
    {
        var client = ClientDoUsuario(_factory.UsuarioBId);

        var response = await client.GetAsync($"/api/calendarioletivo?anoLetivoId={_cenario.AnoLetivoId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<JsonElement> GetAula(HttpClient client, DateOnly data)
        => await client.GetFromJsonAsync<JsonElement>(
            $"/api/chamadas/aula?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.DisciplinaId}&data={data:yyyy-MM-dd}");

    private static DateOnly UltimoDiaUtilAntesDeHoje()
    {
        var dia = Hoje.AddDays(-1);
        while (!CalendarioLetivo.DiaUtil(dia))
            dia = dia.AddDays(-1);
        return dia;
    }

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
    /// Escola B1 com a turma "6º Ano A" (Ana enturmada, Matemática na grade). Um gestor da rede (Gerência) e um
    /// coordenador vinculado à escola (perfil Diretor com escola), ambos com calendário e chamada.
    /// </summary>
    private sealed record Cenario(
        int AnoLetivoId, DateOnly AnoLetivoInicio, int EscolaId, int TurmaId, int DisciplinaId, int AlunoId,
        int GestorId, int CoordenadorEscolaId)
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
                var ana = new Aluno { TenantId = tenantId, Nome = "Ana Souza", Matricula = "1", EscolaId = escola.Id, Status = Aluno.StatusAtivo };
                var gestor = new User { TenantId = tenantId, Email = "gestor@escola-b.com", Cpf = "39053344705" };
                var coordenador = new User { TenantId = tenantId, Email = "coord@escola-b.com", Cpf = "86288366757" };
                var perfilGerencia = new Perfil { Nome = Perfil.Gerencia };
                var perfilDiretor = new Perfil { Nome = Perfil.Diretor };
                db.AddRange(turma, matematica, ana, gestor, coordenador, perfilGerencia, perfilDiretor);
                db.SaveChanges();

                db.AddRange(
                    new DisciplinaEtapaEnsino { TenantId = tenantId, DisciplinaId = matematica.Id, EtapaEnsinoId = etapa.Id },
                    new AlunoTurma { TenantId = tenantId, AlunoId = ana.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new UsuarioPerfil { UsuarioId = gestor.Id, PerfilId = perfilGerencia.Id },
                    new UsuarioPerfil { UsuarioId = coordenador.Id, PerfilId = perfilDiretor.Id, EscolaId = escola.Id });
                db.AddRange(new[] { perfilGerencia, perfilDiretor }.SelectMany(perfil =>
                    new[]
                    {
                        Permissoes.CalendarioLetivo.Visualizar, Permissoes.CalendarioLetivo.Editar,
                        Permissoes.Chamada.Visualizar, Permissoes.Chamada.Criar, Permissoes.Chamada.Editar,
                    }.Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfil.Id, Permissao = p })));
                db.SaveChanges();

                var cenario = new Cenario(ano.Id, ano.DataInicio, escola.Id, turma.Id, matematica.Id, ana.Id, gestor.Id, coordenador.Id);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }
    }
}
