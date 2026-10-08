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
/// Fluxo HTTP das notas: avaliações, lançamento, nota do período, médias, regra da etapa, escopo do
/// professor e a proteção dos períodos do ano letivo, com permissões reais e banco InMemory.
/// </summary>
public class NotasApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public NotasApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task Gestao_CriaAvaliacoesLancaNotasEConsultaMedias()
    {
        // História: o professor do cenário não tem acesso, então os outros testes não interferem aqui.
        var client = ClientDoUsuario(_cenario.SecretarioId);

        var turmas = await client.GetFromJsonAsync<JsonElement>("/api/notas/turmas");
        var turma = Assert.Single(turmas.EnumerateArray());
        Assert.Equal(["História", "Matemática"], turma.GetProperty("disciplinas").EnumerateArray().Select(d => d.GetProperty("nome").GetString()));
        Assert.Equal(4, turma.GetProperty("periodos").GetArrayLength());

        var prova = await CriarAvaliacaoAsync(client, _cenario.HistoriaId, _cenario.Periodo1Id, "Prova 1", "PROVA", peso: 2m);
        var trabalho = await CriarAvaliacaoAsync(client, _cenario.HistoriaId, _cenario.Periodo1Id, "Trabalho", "TRABALHO", peso: 1m);
        var recuperacao = await CriarAvaliacaoAsync(client, _cenario.HistoriaId, _cenario.Periodo1Id, "Recuperação", "RECUPERACAO");

        var outraRecuperacao = await client.PostAsJsonAsync("/api/notas/avaliacoes", CorpoAvaliacao(_cenario.HistoriaId, _cenario.Periodo1Id, "Rec 2", "RECUPERACAO"));
        Assert.Equal(HttpStatusCode.Conflict, outraRecuperacao.StatusCode);

        var acimaDoMaximo = await client.PutAsJsonAsync("/api/notas/lancamentos",
            Lancamento(_cenario.HistoriaId, _cenario.Periodo1Id, (prova, _cenario.AnaId, 11m)));
        Assert.Equal(HttpStatusCode.BadRequest, acimaDoMaximo.StatusCode);

        var lancamento = await client.PutAsJsonAsync("/api/notas/lancamentos", Lancamento(_cenario.HistoriaId, _cenario.Periodo1Id,
            (prova, _cenario.AnaId, 8m), (trabalho, _cenario.AnaId, 5m), (prova, _cenario.BrunoId, 3m), (recuperacao, _cenario.BrunoId, 6.5m)));
        Assert.Equal(HttpStatusCode.OK, lancamento.StatusCode);

        var periodo = await client.GetFromJsonAsync<JsonElement>(
            $"/api/notas/periodo?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}&periodoId={_cenario.Periodo1Id}");
        var ana = LinhaDoAluno(periodo, _cenario.AnaId);
        var bruno = LinhaDoAluno(periodo, _cenario.BrunoId);
        Assert.Equal(7m, ana.GetProperty("notaPeriodo").GetDecimal());
        Assert.Equal((3m, 6.5m, 1), (bruno.GetProperty("media").GetDecimal(), bruno.GetProperty("notaPeriodo").GetDecimal(), bruno.GetProperty("pendentes").GetInt32()));
        Assert.Equal("RECUPERACAO", periodo.GetProperty("avaliacoes").EnumerateArray().Last().GetProperty("tipo").GetString());

        var medias = await client.GetFromJsonAsync<JsonElement>($"/api/notas/medias?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}");
        var mediaAna = LinhaDoAluno(medias, _cenario.AnaId);
        Assert.Equal(7m, mediaAna.GetProperty("mediaFinal").GetDecimal());
        Assert.Equal("EM_ANDAMENTO", mediaAna.GetProperty("situacao").GetString());

        // Apagar a nota (valor nulo) tira o aluno da média do período.
        await client.PutAsJsonAsync("/api/notas/lancamentos", Lancamento(_cenario.HistoriaId, _cenario.Periodo1Id, (trabalho, _cenario.AnaId, null)));
        var depois = await client.GetFromJsonAsync<JsonElement>(
            $"/api/notas/periodo?turmaId={_cenario.TurmaId}&disciplinaId={_cenario.HistoriaId}&periodoId={_cenario.Periodo1Id}");
        Assert.Equal(8m, LinhaDoAluno(depois, _cenario.AnaId).GetProperty("notaPeriodo").GetDecimal());

        var exclusao = await client.DeleteAsync($"/api/notas/avaliacoes/{trabalho}");
        Assert.Equal(HttpStatusCode.OK, exclusao.StatusCode);
    }

    [Fact]
    public async Task RegraDaEtapa_MudaAMediaDaTurma_EExcluirVoltaAoPadrao()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);

        var criacao = await client.PostAsJsonAsync("/api/regras-avaliacao", new
        {
            nome = "Média 7",
            notaMaxima = 10m,
            mediaAprovacao = 7m,
            casasDecimais = 1,
            calculoNotaPeriodo = "MEDIA_PONDERADA",
            permiteRecuperacao = false,
            etapaEnsinoIds = new[] { _cenario.EtapaId },
        });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var regraId = (await criacao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var comRegra = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/notas/turmas")).EnumerateArray()).GetProperty("regra");
        Assert.Equal((regraId, 7m, false), (comRegra.GetProperty("id").GetInt32(), comRegra.GetProperty("mediaAprovacao").GetDecimal(),
            comRegra.GetProperty("permiteRecuperacao").GetBoolean()));

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/regras-avaliacao/{regraId}")).StatusCode);

        var padrao = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/notas/turmas")).EnumerateArray()).GetProperty("regra");
        Assert.Equal(JsonValueKind.Null, padrao.GetProperty("id").ValueKind);
        Assert.Equal(6m, padrao.GetProperty("mediaAprovacao").GetDecimal());
    }

    [Fact]
    public async Task Professor_SoAcessaTurmasEDisciplinasAlocadas()
    {
        var client = ClientDoUsuario(_cenario.ProfessorUsuarioId);

        var turma = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/api/notas/turmas")).EnumerateArray());
        Assert.Equal(["Matemática"], turma.GetProperty("disciplinas").EnumerateArray().Select(d => d.GetProperty("nome").GetString()));

        var naoAlocada = await client.PostAsJsonAsync("/api/notas/avaliacoes", CorpoAvaliacao(_cenario.HistoriaId, _cenario.Periodo2Id, "Prova", "PROVA"));
        var alocada = await client.PostAsJsonAsync("/api/notas/avaliacoes", CorpoAvaliacao(_cenario.MatematicaId, _cenario.Periodo2Id, "Prova", "PROVA"));

        Assert.Equal(HttpStatusCode.Forbidden, naoAlocada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alocada.StatusCode);
    }

    [Fact]
    public async Task AnoLetivo_NaoRemovePeriodoComAvaliacoes_EPreservaOsIdsAoEditar()
    {
        var client = ClientDoUsuario(_cenario.SecretarioId);
        await CriarAvaliacaoAsync(client, _cenario.MatematicaId, _cenario.Periodo3Id, "Prova do 3º", "PROVA");

        var semestral = await client.PutAsJsonAsync($"/api/anosletivos/{_cenario.AnoLetivoId}", CorpoAnoLetivo("SEMESTRAL",
            ("1º Semestre", Hoje.AddDays(-60), Hoje.AddDays(-1)), ("2º Semestre", Hoje, Hoje.AddDays(60))));
        Assert.Equal(HttpStatusCode.BadRequest, semestral.StatusCode);
        Assert.Contains("3º Bimestre", (await semestral.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());

        var renomeado = await client.PutAsJsonAsync($"/api/anosletivos/{_cenario.AnoLetivoId}", CorpoAnoLetivo("BIMESTRAL",
            ("Bimestre I", Hoje.AddDays(-60), Hoje.AddDays(-31)), ("Bimestre II", Hoje.AddDays(-30), Hoje.AddDays(-1)),
            ("Bimestre III", Hoje, Hoje.AddDays(29)), ("Bimestre IV", Hoje.AddDays(30), Hoje.AddDays(60))));
        Assert.Equal(HttpStatusCode.OK, renomeado.StatusCode);

        var periodos = (await renomeado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("periodos").EnumerateArray().ToList();
        Assert.Equal(_cenario.Periodo3Id, periodos.Single(p => p.GetProperty("numero").GetInt32() == 3).GetProperty("id").GetInt32());
        Assert.Equal("Bimestre III", periodos.Single(p => p.GetProperty("numero").GetInt32() == 3).GetProperty("nome").GetString());
    }

    [Fact]
    public async Task UsuarioSemPermissaoDeNotas_Recebe403()
    {
        var client = ClientDoUsuario(_factory.UsuarioBId);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/notas/turmas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/regras-avaliacao")).StatusCode);
    }

    private async Task<int> CriarAvaliacaoAsync(HttpClient client, int disciplinaId, int periodoId, string nome, string tipo, decimal? peso = null)
    {
        var response = await client.PostAsJsonAsync("/api/notas/avaliacoes", CorpoAvaliacao(disciplinaId, periodoId, nome, tipo, peso));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private object CorpoAvaliacao(int disciplinaId, int periodoId, string nome, string tipo, decimal? peso = null) => new
    {
        turmaId = _cenario.TurmaId,
        disciplinaId,
        periodoAvaliativoId = periodoId,
        nome,
        tipo,
        peso,
    };

    private object Lancamento(int disciplinaId, int periodoId, params (int AvaliacaoId, int AlunoId, decimal? Valor)[] notas) => new
    {
        turmaId = _cenario.TurmaId,
        disciplinaId,
        periodoAvaliativoId = periodoId,
        notas = notas.Select(n => new { avaliacaoId = n.AvaliacaoId, alunoId = n.AlunoId, valor = n.Valor }).ToArray(),
    };

    private static object CorpoAnoLetivo(string tipo, params (string Nome, DateOnly Inicio, DateOnly Termino)[] periodos) => new
    {
        anoReferencia = Hoje.Year,
        dataInicio = Hoje.AddDays(-60).ToString("yyyy-MM-dd"),
        dataTermino = Hoje.AddDays(60).ToString("yyyy-MM-dd"),
        tipoPeriodo = tipo,
        periodos = periodos.Select((p, i) => new
        {
            nome = p.Nome,
            numero = i + 1,
            dataInicio = p.Inicio.ToString("yyyy-MM-dd"),
            dataTermino = p.Termino.ToString("yyyy-MM-dd"),
        }).ToArray(),
    };

    private static JsonElement LinhaDoAluno(JsonElement resposta, int alunoId)
        => resposta.GetProperty("alunos").EnumerateArray().Single(a => a.GetProperty("alunoId").GetInt32() == alunoId);

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
    /// Escola B: turma "6º Ano A" com Ana e Bruno; Matemática e História na grade; ano com 4 bimestres
    /// (os dois primeiros já passaram). Um secretário com as permissões de notas, regras e ano letivo e
    /// um professor alocado só em Matemática.
    /// </summary>
    private sealed record Cenario(
        int TurmaId, int EtapaId, int AnoLetivoId, int Periodo1Id, int Periodo2Id, int Periodo3Id,
        int MatematicaId, int HistoriaId, int AnaId, int BrunoId, int SecretarioId, int ProfessorUsuarioId)
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
                var etapa = new EtapaEnsino { TenantId = tenantId, ModalidadeEnsinoId = factory.ModalidadeBId, Nome = "6º Ano", Sigla = "6A", OrdemCronologica = 6 };
                var ano = new AnoLetivo
                {
                    TenantId = tenantId, AnoReferencia = Hoje.Year, TipoPeriodo = AnoLetivo.TipoBimestral,
                    DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(60),
                    Periodos =
                    [
                        new PeriodoAvaliativo { TenantId = tenantId, Nome = "1º Bimestre", Numero = 1, DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(-31) },
                        new PeriodoAvaliativo { TenantId = tenantId, Nome = "2º Bimestre", Numero = 2, DataInicio = Hoje.AddDays(-30), DataTermino = Hoje.AddDays(-1) },
                        new PeriodoAvaliativo { TenantId = tenantId, Nome = "3º Bimestre", Numero = 3, DataInicio = Hoje, DataTermino = Hoje.AddDays(29) },
                        new PeriodoAvaliativo { TenantId = tenantId, Nome = "4º Bimestre", Numero = 4, DataInicio = Hoje.AddDays(30), DataTermino = Hoje.AddDays(60) },
                    ],
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
                professor.ProfessorEscolas.Add(new ProfessorEscola { TenantId = tenantId, EscolaId = escola.Id });
                db.Add(professor);
                db.AddRange(
                    new DisciplinaEtapaEnsino { TenantId = tenantId, DisciplinaId = matematica.Id, EtapaEnsinoId = etapa.Id },
                    new DisciplinaEtapaEnsino { TenantId = tenantId, DisciplinaId = historia.Id, EtapaEnsinoId = etapa.Id },
                    new AlunoTurma { TenantId = tenantId, AlunoId = ana.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new AlunoTurma { TenantId = tenantId, AlunoId = bruno.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new UsuarioPerfil { UsuarioId = secretario.Id, PerfilId = perfilSecretario.Id },
                    new UsuarioPerfil { UsuarioId = usuarioProfessor.Id, PerfilId = perfilProfessor.Id });

                string[] notas = [Permissoes.Notas.Visualizar, Permissoes.Notas.Criar, Permissoes.Notas.Editar, Permissoes.Notas.Excluir];
                string[] gestao =
                [
                    Permissoes.RegrasAvaliacao.Visualizar, Permissoes.RegrasAvaliacao.Criar, Permissoes.RegrasAvaliacao.Editar,
                    Permissoes.RegrasAvaliacao.Excluir, Permissoes.AnosLetivos.Editar, "anos-letivos.visualizar",
                ];
                db.AddRange(notas.Concat(gestao).Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilSecretario.Id, Permissao = p }));
                db.AddRange(notas.Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilProfessor.Id, Permissao = p }));
                db.SaveChanges();

                db.Add(new ProfessorAlocacao { TenantId = tenantId, ProfessorId = professor.Id, TurmaId = turma.Id, DisciplinaId = matematica.Id });
                db.SaveChanges();

                var periodos = ano.Periodos.OrderBy(p => p.Numero).Select(p => p.Id).ToList();
                var cenario = new Cenario(turma.Id, etapa.Id, ano.Id, periodos[0], periodos[1], periodos[2],
                    matematica.Id, historia.Id, ana.Id, bruno.Id, secretario.Id, usuarioProfessor.Id);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }
    }
}
