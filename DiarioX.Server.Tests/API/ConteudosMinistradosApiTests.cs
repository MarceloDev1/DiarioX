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
/// Fluxo HTTP do conteúdo ministrado: catálogo BNCC e sugestões (RN02), registro, diário que cruza com a
/// chamada (RN01), bloqueio do calendário (EX01) e escopo do professor, com permissões reais e banco InMemory.
/// </summary>
public class ConteudosMinistradosApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public ConteudosMinistradosApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task Gestao_CadastraHabilidade_RecebeSugestaoERegistraConteudo()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);

        var criacao = await client.PostAsJsonAsync("/api/habilidadesbncc", new
        {
            codigo = "ef06ma01",
            descricao = "Comparar, ordenar, ler e escrever números naturais e números racionais.",
            disciplinaId = _cenario.MatematicaId,
            etapasEnsinoIds = new[] { _cenario.EtapaId },
        });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var habilidade = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var habilidadeId = habilidade.GetProperty("id").GetInt32();
        Assert.Equal("EF06MA01", habilidade.GetProperty("codigo").GetString());

        var duplicada = await client.PostAsJsonAsync("/api/habilidadesbncc", new
        {
            codigo = "EF06MA01", descricao = "Outra", disciplinaId = _cenario.MatematicaId, etapasEnsinoIds = new[] { _cenario.EtapaId },
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);

        // RN02: só as habilidades da disciplina e da etapa da turma.
        var sugeridas = await client.GetFromJsonAsync<JsonElement>(
            $"/api/conteudosministrados/sugestoes-bncc?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.MatematicaId}");
        Assert.Equal(["EF06MA01"], sugeridas.EnumerateArray().Select(h => h.GetProperty("codigo").GetString()));
        var deHistoria = await client.GetFromJsonAsync<JsonElement>(
            $"/api/conteudosministrados/sugestoes-bncc?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}");
        Assert.Equal(0, deHistoria.GetArrayLength());
        var semResultado = await client.GetFromJsonAsync<JsonElement>(
            $"/api/conteudosministrados/sugestoes-bncc?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.MatematicaId}&busca=geometria");
        Assert.Equal(0, semResultado.GetArrayLength());

        var data = DiaUtilAte(Hoje.AddDays(-5));
        var registro = await client.PostAsJsonAsync("/api/conteudosministrados", Corpo(_cenario.MatematicaId, data, "Frações", habilidadeId));
        Assert.Equal(HttpStatusCode.OK, registro.StatusCode);
        var conteudo = await registro.Content.ReadFromJsonAsync<JsonElement>();
        var conteudoId = conteudo.GetProperty("conteudoId").GetInt32();
        Assert.Equal("sec@escola-b.com", conteudo.GetProperty("registradoPor").GetString());
        Assert.Equal(["EF06MA01"], conteudo.GetProperty("habilidades").EnumerateArray().Select(h => h.GetProperty("codigo").GetString()));

        var repetido = await client.PostAsJsonAsync("/api/conteudosministrados", Corpo(_cenario.MatematicaId, data, "Outro"));
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        var alteracao = await client.PutAsJsonAsync($"/api/conteudosministrados/{conteudoId}", Corpo(_cenario.MatematicaId, data, "Frações equivalentes"));
        Assert.Equal(HttpStatusCode.OK, alteracao.StatusCode);
        var alterado = await alteracao.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Frações equivalentes", alterado.GetProperty("descricao").GetString());
        Assert.Equal(0, alterado.GetProperty("habilidades").GetArrayLength());

        // Habilidade usada num conteúdo não pode ser excluída; depois de desvinculada pode.
        var vinculo = await client.PutAsJsonAsync($"/api/conteudosministrados/{conteudoId}", Corpo(_cenario.MatematicaId, data, "Frações", habilidadeId));
        Assert.Equal(HttpStatusCode.OK, vinculo.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/habilidadesbncc/{habilidadeId}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/conteudosministrados/{conteudoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/habilidadesbncc/{habilidadeId}")).StatusCode);
    }

    [Fact]
    public async Task Diario_SinalizaFrequenciaSemConteudoEConteudoSemFrequencia()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);
        var completo = DiaUtilAte(Hoje.AddDays(-10));
        var soFrequencia = DiaUtilAte(completo.AddDays(-1));
        var soConteudo = DiaUtilAte(soFrequencia.AddDays(-1));

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/chamadas", CorpoChamada(_cenario.HistoriaId, completo))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/chamadas", CorpoChamada(_cenario.HistoriaId, soFrequencia))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/conteudosministrados", Corpo(_cenario.HistoriaId, completo, "Brasil Colônia"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/conteudosministrados", Corpo(_cenario.HistoriaId, soConteudo, "Capitanias"))).StatusCode);

        var diario = await client.GetFromJsonAsync<JsonElement>(
            $"/api/conteudosministrados/diario?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}");

        var dias = diario.GetProperty("dias").EnumerateArray().ToDictionary(
            d => DateOnly.Parse(d.GetProperty("data").GetString()!), d => d);
        Assert.Equal(JsonValueKind.Null, dias[completo].GetProperty("pendencia").ValueKind);
        Assert.Equal("SEM_CONTEUDO", dias[soFrequencia].GetProperty("pendencia").GetString());
        Assert.Equal("SEM_FREQUENCIA", dias[soConteudo].GetProperty("pendencia").GetString());
        Assert.Equal(2, diario.GetProperty("totalPendencias").GetInt32());
    }

    [Fact]
    public async Task DiaSemAulaNoCalendarioPublicado_BloqueiaRegistro()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);
        var feriado = Hoje.AddDays(-20);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var calendario = new CalendarioLetivo
            {
                TenantId = _factory.TenantB.Id, AnoLetivoId = _cenario.AnoLetivoId, PublicadoEm = DateTime.UtcNow,
            };
            calendario.Eventos.Add(new EventoCalendario
            {
                TenantId = _factory.TenantB.Id, Data = feriado, Tipo = EventoCalendario.TipoConselhoClasse,
                Descricao = "Conselho de Classe", ComAula = false,
            });
            db.Add(calendario);
            db.SaveChanges();
        }

        var resposta = await client.PostAsJsonAsync("/api/conteudosministrados", Corpo(_cenario.MatematicaId, feriado, "Frações"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Conselho de Classe - Dia Sem Aula", erro.GetProperty("message").GetString());

        var aula = await client.GetFromJsonAsync<JsonElement>(
            $"/api/conteudosministrados/aula?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.MatematicaId}&data={feriado:yyyy-MM-dd}");
        Assert.Contains("Conselho de Classe", aula.GetProperty("bloqueio").GetString());
    }

    [Fact]
    public async Task Professor_SoRegistraConteudoDasDisciplinasAlocadas()
    {
        var client = ClientDoUsuario(_cenario.ProfessorUsuarioId);

        var turmas = await client.GetFromJsonAsync<JsonElement>("/api/conteudosministrados/turmas");
        var disciplinas = turmas.EnumerateArray().Single().GetProperty("disciplinas").EnumerateArray()
            .Select(d => d.GetProperty("nome").GetString());
        Assert.Equal(["Matemática"], disciplinas);

        var proibido = await client.PostAsJsonAsync("/api/conteudosministrados", Corpo(_cenario.HistoriaId, Hoje.AddDays(-2), "Brasil"));
        Assert.Equal(HttpStatusCode.Forbidden, proibido.StatusCode);

        // O professor não cadastra habilidades (só consulta).
        var cadastro = await client.PostAsJsonAsync("/api/habilidadesbncc", new
        {
            codigo = "EF06MA09", descricao = "x", disciplinaId = _cenario.MatematicaId, etapasEnsinoIds = new[] { _cenario.EtapaId },
        });
        Assert.Equal(HttpStatusCode.Forbidden, cadastro.StatusCode);
    }

    /// <summary>Sem calendário publicado só segunda a sexta é dia letivo: recua até o dia útil mais próximo.</summary>
    private static DateOnly DiaUtilAte(DateOnly data)
    {
        while (data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            data = data.AddDays(-1);
        return data;
    }

    private object Corpo(int disciplinaId, DateOnly data, string descricao, params int[] habilidadesIds) => new
    {
        turmaId = _cenario.TurmaId,
        disciplinaId,
        data = data.ToString("yyyy-MM-dd"),
        descricao,
        habilidadesIds,
    };

    private object CorpoChamada(int disciplinaId, DateOnly data) => new
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
    /// Escola B: turma "6º Ano A" com Ana e Bruno; Matemática e História na grade. Um secretário (gestão, com
    /// chamada, conteúdo e BNCC) e um professor alocado só em Matemática (conteúdo e consulta da BNCC).
    /// </summary>
    private sealed record Cenario(
        int TurmaId, int EtapaId, int AnoLetivoId, int MatematicaId, int HistoriaId, int AnaId, int BrunoId, int SecretarioId, int ProfessorUsuarioId)
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
                    DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(30),
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

                string[] gestao =
                [
                    Permissoes.Chamada.Visualizar, Permissoes.Chamada.Criar, Permissoes.Chamada.Editar,
                    Permissoes.ConteudoMinistrado.Visualizar, Permissoes.ConteudoMinistrado.Criar,
                    Permissoes.ConteudoMinistrado.Editar, Permissoes.ConteudoMinistrado.Excluir,
                    Permissoes.HabilidadesBncc.Visualizar, Permissoes.HabilidadesBncc.Criar,
                    Permissoes.HabilidadesBncc.Editar, Permissoes.HabilidadesBncc.Excluir,
                ];
                string[] docente =
                [
                    Permissoes.ConteudoMinistrado.Visualizar, Permissoes.ConteudoMinistrado.Criar,
                    Permissoes.ConteudoMinistrado.Editar, Permissoes.HabilidadesBncc.Visualizar,
                ];
                db.AddRange(gestao.Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilSecretario.Id, Permissao = p }));
                db.AddRange(docente.Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilProfessor.Id, Permissao = p }));
                db.SaveChanges();

                db.Add(new ProfessorAlocacao { TenantId = tenantId, ProfessorId = professor.Id, TurmaId = turma.Id, DisciplinaId = matematica.Id });
                db.SaveChanges();

                var cenario = new Cenario(turma.Id, etapa.Id, ano.Id, matematica.Id, historia.Id, ana.Id, bruno.Id, secretario.Id, usuarioProfessor.Id);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }
    }
}
