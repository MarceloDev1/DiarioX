using System.IdentityModel.Tokens.Jwt;
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
/// Painel da home pela API real: consultas no banco, permissões por bloco e restrição do professor às
/// próprias turmas/disciplinas.
/// </summary>
public class DashboardApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public DashboardApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task Gerencia_VeIndicadoresAlertasEUltimasChamadas()
    {
        var painel = await ClientDoUsuario(_cenario.GerenciaId).GetFromJsonAsync<JsonElement>("/api/dashboard");

        // O aluno inativo não conta.
        Assert.Equal(2, painel.GetProperty("alunos").GetProperty("ativos").GetInt32());
        Assert.Equal(1, painel.GetProperty("turmas").GetProperty("ativas").GetInt32());
        Assert.Equal(2, painel.GetProperty("professores").GetProperty("alocados").GetInt32());
        // 4 aulas: um aluno presente e um com falta.
        Assert.Equal(50m, painel.GetProperty("frequencia").GetProperty("percentual").GetDecimal());

        var alertas = Mensagens(painel);
        Assert.Contains("Atenção: 1 turma sem chamada há mais de 7 dias (1 disciplina).", alertas);
        Assert.Contains("1 aluno está com frequência abaixo de 75% nos últimos 30 dias.", alertas);
        Assert.Contains(alertas, a => a.StartsWith("1º Bimestre de ", StringComparison.Ordinal));

        var chamada = Assert.Single(painel.GetProperty("ultimasChamadas").EnumerateArray());
        Assert.Equal("Carla Professora", chamada.GetProperty("registradoPor").GetString());
        Assert.Equal(1, chamada.GetProperty("presentes").GetInt32());
        Assert.Equal(2, chamada.GetProperty("alunos").GetInt32());
    }

    [Fact]
    public async Task Professor_VeSoAChamadaDasPropriasAlocacoesESemBlocosSemPermissao()
    {
        var painel = await ClientDoUsuario(_cenario.ProfessorUsuarioId).GetFromJsonAsync<JsonElement>("/api/dashboard");

        Assert.Equal(JsonValueKind.Null, painel.GetProperty("alunos").ValueKind);
        Assert.Equal(JsonValueKind.Null, painel.GetProperty("professores").ValueKind);
        Assert.Single(painel.GetProperty("ultimasChamadas").EnumerateArray());
        // A disciplina sem chamada é de outro professor.
        Assert.DoesNotContain(Mensagens(painel), a => a.Contains("sem chamada", StringComparison.Ordinal));
    }

    private static List<string> Mensagens(JsonElement painel)
        => painel.GetProperty("alertas").EnumerateArray().Select(a => a.GetProperty("mensagem").GetString()!).ToList();

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
    /// Uma turma com 2 alunos ativos e 1 inativo, duas disciplinas com professores diferentes. Só a
    /// disciplina da professora Carla tem chamada (há 3 dias, 4 aulas: Ana presente, Bruno faltou).
    /// O 1º bimestre termina em 5 dias.
    /// </summary>
    private sealed record Cenario(int GerenciaId, int ProfessorUsuarioId)
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

                var escola = new Escola { TenantId = tenantId, Nome = "Escola Norte", CodigoInep = "10" };
                var etapa = new EtapaEnsino { TenantId = tenantId, ModalidadeEnsinoId = factory.ModalidadeBId, Nome = "1º Ano", Sigla = "1A" };
                var ano = new AnoLetivo
                {
                    TenantId = tenantId, AnoReferencia = hoje.Year, TipoPeriodo = AnoLetivo.TipoBimestral,
                    DataInicio = hoje.AddDays(-60), DataTermino = hoje.AddDays(200),
                    Periodos =
                    {
                        new PeriodoAvaliativo { TenantId = tenantId, Nome = "1º Bimestre", Numero = 1, DataInicio = hoje.AddDays(-60), DataTermino = hoje.AddDays(5) },
                    },
                };
                var matematica = new Disciplina { TenantId = tenantId, Nome = "Matemática", Codigo = "MAT" };
                var historia = new Disciplina { TenantId = tenantId, Nome = "História", Codigo = "HIS" };
                var gerencia = new User { TenantId = tenantId, Email = "gerencia@rede.com", Cpf = "86288366757" };
                var usuarioCarla = new User { TenantId = tenantId, Email = "carla@rede.com", Cpf = "39053344705" };
                var perfilGerencia = new Perfil { Nome = Perfil.Gerencia };
                var perfilProfessor = new Perfil { Nome = Perfil.Professor };
                db.AddRange(escola, etapa, ano, matematica, historia, gerencia, usuarioCarla, perfilGerencia, perfilProfessor);
                db.SaveChanges();

                var turma = new Turma
                {
                    TenantId = tenantId, AnoLetivoId = ano.Id, EscolaId = escola.Id, ModalidadeEnsinoId = factory.ModalidadeBId,
                    EtapaEnsinoId = etapa.Id, NomeIdentificador = "A", NomeCompleto = "1º Ano A", VagasOfertadas = 30,
                };
                var carla = NovoProfessor(tenantId, "Carla Professora", "11122233396", usuarioCarla.Id);
                var davi = NovoProfessor(tenantId, "Davi Professor", "52998224725", null);
                var ana = NovoAluno(tenantId, escola.Id, "Ana", "1", Aluno.StatusAtivo);
                var bruno = NovoAluno(tenantId, escola.Id, "Bruno", "2", Aluno.StatusAtivo);
                var inativo = NovoAluno(tenantId, escola.Id, "Caio", "3", Aluno.StatusInativo);
                db.AddRange(turma, carla, davi, ana, bruno, inativo);
                db.SaveChanges();

                db.AddRange(
                    new ProfessorEscola { TenantId = tenantId, ProfessorId = carla.Id, EscolaId = escola.Id },
                    new ProfessorEscola { TenantId = tenantId, ProfessorId = davi.Id, EscolaId = escola.Id },
                    new ProfessorAlocacao { TenantId = tenantId, ProfessorId = carla.Id, TurmaId = turma.Id, DisciplinaId = matematica.Id },
                    new ProfessorAlocacao { TenantId = tenantId, ProfessorId = davi.Id, TurmaId = turma.Id, DisciplinaId = historia.Id },
                    new AlunoTurma { TenantId = tenantId, AlunoId = ana.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new AlunoTurma { TenantId = tenantId, AlunoId = bruno.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new AlunoTurma { TenantId = tenantId, AlunoId = inativo.Id, TurmaId = turma.Id, DataInicio = ano.DataInicio },
                    new Chamada
                    {
                        TenantId = tenantId, TurmaId = turma.Id, DisciplinaId = matematica.Id, Data = hoje.AddDays(-3),
                        QuantidadeAulas = 4, RegistradoPorUsuarioId = usuarioCarla.Id,
                        Registros =
                        {
                            new ChamadaAluno { TenantId = tenantId, AlunoId = ana.Id, Situacao = ChamadaAluno.SituacaoPresente },
                            new ChamadaAluno { TenantId = tenantId, AlunoId = bruno.Id, Situacao = ChamadaAluno.SituacaoFalta },
                        },
                    },
                    new UsuarioPerfil { UsuarioId = gerencia.Id, PerfilId = perfilGerencia.Id },
                    new UsuarioPerfil { UsuarioId = usuarioCarla.Id, PerfilId = perfilProfessor.Id });
                db.AddRange(
                    new[] { Permissoes.Alunos.Visualizar, Permissoes.Turmas.Visualizar, Permissoes.Chamada.Visualizar, Permissoes.Professores.Visualizar }
                        .Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilGerencia.Id, Permissao = p })
                        .Append(new PerfilPermissao { TenantId = tenantId, PerfilId = perfilProfessor.Id, Permissao = Permissoes.Chamada.Visualizar }));
                db.SaveChanges();

                var cenario = new Cenario(gerencia.Id, usuarioCarla.Id);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }

        private static Professor NovoProfessor(int tenantId, string nome, string cpf, int? usuarioId) => new()
        {
            TenantId = tenantId, Nome = nome, Cpf = cpf, Email = $"{cpf}@rede.com", Telefone = "11987654321",
            DataNascimento = new DateTime(1985, 5, 15), UsuarioId = usuarioId,
        };

        private static Aluno NovoAluno(int tenantId, int escolaId, string nome, string matricula, string status) => new()
        {
            TenantId = tenantId, EscolaId = escolaId, Nome = nome, Matricula = matricula, Status = status,
            Sexo = Aluno.SexoFeminino, DataNascimento = new DateTime(2019, 3, 10),
            ResponsavelNome1 = "Responsável", ResponsavelTelefone1 = "11987654321",
        };
    }
}
