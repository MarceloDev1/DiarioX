using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DiarioX.Server.Tests.API;

/// <summary>
/// Home do professor e grade de horários pela API real: aulas previstas (grade × calendário publicado),
/// pendências do mês, gráficos, pendências de notas por período e as exceções EX01/EX02.
/// </summary>
public class PainelProfessorApiTests : IClassFixture<TenancyApiFactory>
{
    private const string HostEscolaB = "http://escola-b.dev.localhost";

    private readonly TenancyApiFactory _factory;
    private readonly Cenario _cenario;

    public PainelProfessorApiTests(TenancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _cenario = Cenario.Obter(factory);
    }

    [Fact]
    public async Task UsuarioSemCadastroDeProfessor_RecebeNoContent()
    {
        var response = await ClientDoUsuario(_cenario.GerenciaId).GetAsync("/api/dashboard/professor");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ProfessorSemLotacaoNoAno_RecebeMensagemEx02()
    {
        var painel = await ClientDoUsuario(_cenario.SemLotacaoUsuarioId).GetFromJsonAsync<JsonElement>("/api/dashboard/professor");

        Assert.Equal(PainelProfessorService.MensagemSemLotacao, painel.GetProperty("semLotacao").GetString());
        Assert.Empty(painel.GetProperty("diarios").EnumerateArray());
    }

    [Fact]
    public async Task ProfessorSemVinculoNemEscola_EncontradoPeloEmail_VeOsPropriosDiarios()
    {
        var painel = await ClientDoUsuario(_cenario.SemVinculoUsuarioId).GetFromJsonAsync<JsonElement>("/api/dashboard/professor");

        Assert.Equal("Fabio Professor", painel.GetProperty("professorNome").GetString());
        var diario = Assert.Single(painel.GetProperty("diarios").EnumerateArray());
        Assert.Equal("6º Ano B", diario.GetProperty("turmaNome").GetString());
        Assert.True(diario.GetProperty("semGrade").GetBoolean());
    }

    [Fact]
    public async Task PerfilProfessorSemCadastro_VeHomeDoProfessorComEx02()
    {
        var response = await ClientDoUsuario(_cenario.SemCadastroUsuarioId).GetAsync("/api/dashboard/professor");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var painel = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(PainelProfessorService.MensagemSemLotacao, painel.GetProperty("semLotacao").GetString());
    }

    [Fact]
    public async Task Professor_VeDiariosAulasDoDiaEGraficos()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var painel = await ClientDoUsuario(_cenario.ProfessorUsuarioId).GetFromJsonAsync<JsonElement>("/api/dashboard/professor");

        Assert.Equal(JsonValueKind.Null, painel.GetProperty("semLotacao").ValueKind);

        // Só a disciplina da professora: História é de outro professor.
        var diario = Assert.Single(painel.GetProperty("diarios").EnumerateArray());
        var disciplina = Assert.Single(diario.GetProperty("disciplinas").EnumerateArray());
        Assert.Equal("Matemática", disciplina.GetProperty("nome").GetString());
        Assert.Equal(12, disciplina.GetProperty("aulasSemanais").GetInt32());
        Assert.False(diario.GetProperty("semGrade").GetBoolean());

        var aulasHoje = painel.GetProperty("hoje").GetProperty("aulas").EnumerateArray().ToList();
        if (CalendarioLetivo.DiaUtil(hoje) && hoje != _cenario.Feriado)
        {
            var aula = Assert.Single(aulasHoje);
            Assert.Equal([1, 2], aula.GetProperty("tempos").EnumerateArray().Select(t => t.GetInt32()));
        }
        else
        {
            // EX01: sem aula no dia.
            Assert.Empty(aulasHoje);
            Assert.Equal(PainelProfessorService.MensagemSemAulas, painel.GetProperty("hoje").GetProperty("mensagem").GetString());
        }

        // RN02: dias úteis passados do ano letivo, menos o feriado do calendário publicado.
        var decorridos = DiasUteis(_cenario.InicioAno, hoje.AddDays(-1)).Count(d => d != _cenario.Feriado);
        var frequencia = painel.GetProperty("registrosFrequencia");
        Assert.Equal(2, frequencia.GetProperty("registrados").GetInt32());
        Assert.Equal(decorridos - 2, frequencia.GetProperty("pendentes").GetInt32());

        var previstasNoAno = DiasUteis(_cenario.InicioAno, _cenario.TerminoAno).Count(d => d != _cenario.Feriado);
        var aulas = painel.GetProperty("registrosAula");
        Assert.Equal(1, aulas.GetProperty("registrados").GetInt32());
        Assert.Equal(decorridos - 1, aulas.GetProperty("pendentes").GetInt32());
        Assert.Equal(previstasNoAno, aulas.GetProperty("registrados").GetInt32() + aulas.GetProperty("pendentes").GetInt32() +
                                     aulas.GetProperty("aFazer").GetInt32());
    }

    [Fact]
    public async Task PendenciasDoMes_DestacaDiasSemRegistroENaoLetivos()
    {
        var client = ClientDoUsuario(_cenario.ProfessorUsuarioId);

        var emDia = await Dia(client, _cenario.ComTudo);
        Assert.Equal("em-dia", emDia.GetProperty("situacao").GetString());

        // RN03: chamada sem conteúdo ministrado.
        var pendente = await Dia(client, _cenario.SoChamada);
        Assert.Equal("pendente", pendente.GetProperty("situacao").GetString());
        var item = Assert.Single(pendente.GetProperty("pendencias").EnumerateArray());
        Assert.False(item.GetProperty("semFrequencia").GetBoolean());
        Assert.True(item.GetProperty("semAula").GetBoolean());

        var feriado = await Dia(client, _cenario.Feriado);
        Assert.Equal("nao-letivo", feriado.GetProperty("situacao").GetString());
        Assert.Equal("Feriado: Padroeira", feriado.GetProperty("evento").GetString());
    }

    [Fact]
    public async Task NotasPendentes_SoPeriodosIniciados()
    {
        var painel = await ClientDoUsuario(_cenario.ProfessorUsuarioId).GetFromJsonAsync<JsonElement>("/api/dashboard/professor");

        // RN01: o 2º período ainda não começou.
        var periodo = Assert.Single(painel.GetProperty("notas").EnumerateArray());
        Assert.Equal("1º Bimestre", periodo.GetProperty("nome").GetString());
        Assert.True(periodo.GetProperty("atual").GetBoolean());

        var pendencia = Assert.Single(periodo.GetProperty("pendencias").EnumerateArray());
        Assert.Equal("1 nota não lançada em 1 avaliação.", pendencia.GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task Horarios_ValidaESubstituiAGrade()
    {
        var gerencia = ClientDoUsuario(_cenario.GerenciaId);
        var url = $"/api/horarios/turmas/{_cenario.OutraTurmaId}";

        var invalido = await gerencia.PutAsJsonAsync(url, new { tempos = new[] { new { diaSemana = 0, ordem = 1, disciplinaId = _cenario.MatematicaId } } });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var salvo = await gerencia.PutAsJsonAsync(url, new
        {
            tempos = new[]
            {
                new { diaSemana = 1, ordem = 1, disciplinaId = _cenario.MatematicaId },
                new { diaSemana = 1, ordem = 2, disciplinaId = _cenario.MatematicaId },
            },
        });
        Assert.Equal(HttpStatusCode.OK, salvo.StatusCode);

        var substituido = await gerencia.PutAsJsonAsync(url, new { tempos = new[] { new { diaSemana = 3, ordem = 1, disciplinaId = _cenario.MatematicaId } } });
        var grade = await substituido.Content.ReadFromJsonAsync<JsonElement>();
        var tempo = Assert.Single(grade.GetProperty("tempos").EnumerateArray());
        Assert.Equal(3, tempo.GetProperty("diaSemana").GetInt32());

        // O professor só consulta a grade.
        var professor = await ClientDoUsuario(_cenario.ProfessorUsuarioId).PutAsJsonAsync(url, new { tempos = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Forbidden, professor.StatusCode);
    }

    private static async Task<JsonElement> Dia(HttpClient client, DateOnly data)
    {
        var mes = await client.GetFromJsonAsync<JsonElement>($"/api/dashboard/professor/pendencias?ano={data.Year}&mes={data.Month}");
        return mes.GetProperty("dias").EnumerateArray().Single(d => d.GetProperty("data").GetString() == data.ToString("yyyy-MM-dd"));
    }

    private static IEnumerable<DateOnly> DiasUteis(DateOnly de, DateOnly ate)
    {
        for (var data = de; data <= ate; data = data.AddDays(1))
        {
            if (CalendarioLetivo.DiaUtil(data))
                yield return data;
        }
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
    /// Turma de frequência por aula com Matemática (Carla) no 1º e 2º tempos de segunda a sábado e História
    /// (Davi) no 3º. Calendário da rede publicado com um feriado. Nos dois dias úteis anteriores ao feriado
    /// há registro: no mais antigo, chamada e conteúdo; no outro, só chamada. O 1º bimestre está em
    /// andamento, com uma avaliação em que só um dos dois alunos tem nota; o 2º ainda não começou.
    /// </summary>
    private sealed record Cenario(
        int GerenciaId,
        int ProfessorUsuarioId,
        int SemLotacaoUsuarioId,
        int SemVinculoUsuarioId,
        int SemCadastroUsuarioId,
        int MatematicaId,
        int OutraTurmaId,
        DateOnly InicioAno,
        DateOnly TerminoAno,
        DateOnly Feriado,
        DateOnly ComTudo,
        DateOnly SoChamada)
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

                // Três dias úteis antes de hoje: feriado no mais recente, registros nos dois anteriores.
                var uteis = Enumerable.Range(1, 10).Select(i => hoje.AddDays(-i)).Where(CalendarioLetivo.DiaUtil).Take(3).ToList();
                var (feriado, soChamada, comTudo) = (uteis[0], uteis[1], uteis[2]);
                var inicio = hoje.AddDays(-20);
                var termino = hoje.AddDays(40);

                var escola = new Escola { TenantId = tenantId, Nome = "Escola Sul", CodigoInep = "20" };
                var etapa = new EtapaEnsino { TenantId = tenantId, ModalidadeEnsinoId = factory.ModalidadeBId, Nome = "6º Ano", Sigla = "6A" };
                var ano = new AnoLetivo
                {
                    TenantId = tenantId, AnoReferencia = hoje.Year, TipoPeriodo = AnoLetivo.TipoBimestral,
                    DataInicio = inicio, DataTermino = termino,
                    Periodos =
                    {
                        new PeriodoAvaliativo { TenantId = tenantId, Nome = "1º Bimestre", Numero = 1, DataInicio = inicio, DataTermino = hoje.AddDays(10) },
                        new PeriodoAvaliativo { TenantId = tenantId, Nome = "2º Bimestre", Numero = 2, DataInicio = hoje.AddDays(11), DataTermino = termino },
                    },
                };
                var matematica = new Disciplina { TenantId = tenantId, Nome = "Matemática", Codigo = "MAT" };
                var historia = new Disciplina { TenantId = tenantId, Nome = "História", Codigo = "HIS" };
                var gerencia = new User { TenantId = tenantId, Email = "gerencia@sul.com", Cpf = "86288366757" };
                var usuarioCarla = new User { TenantId = tenantId, Email = "carla@sul.com", Cpf = "39053344705" };
                var usuarioEva = new User { TenantId = tenantId, Email = "eva@sul.com", Cpf = "71428793860" };
                var usuarioFabio = new User { TenantId = tenantId, Email = "fabio@sul.com", Cpf = "24843803483" };
                var usuarioGil = new User { TenantId = tenantId, Email = "gil@sul.com", Cpf = "86734718697" };
                var perfilGerencia = new Perfil { Nome = Perfil.Gerencia };
                var perfilProfessor = new Perfil { Nome = Perfil.Professor };
                db.AddRange(escola, etapa, ano, matematica, historia, gerencia, usuarioCarla, usuarioEva, usuarioFabio, usuarioGil,
                    perfilGerencia, perfilProfessor);
                db.SaveChanges();

                var turma = NovaTurma(tenantId, ano.Id, escola.Id, factory.ModalidadeBId, etapa.Id, "A");
                var outraTurma = NovaTurma(tenantId, ano.Id, escola.Id, factory.ModalidadeBId, etapa.Id, "B");
                var carla = NovoProfessor(tenantId, "Carla Professora", "11122233396", usuarioCarla.Id);
                var davi = NovoProfessor(tenantId, "Davi Professor", "52998224725", null);
                var eva = NovoProfessor(tenantId, "Eva Professora", "15350946056", usuarioEva.Id);
                // Cadastro sem vínculo com o usuário (a conta já existia) e sem escola de atuação marcada.
                var fabio = NovoProfessor(tenantId, "Fabio Professor", "62648716050", null);
                fabio.Email = "Fabio@Sul.com";
                var ana = NovoAluno(tenantId, escola.Id, "Ana", "1");
                var bruno = NovoAluno(tenantId, escola.Id, "Bruno", "2");
                db.AddRange(turma, outraTurma, carla, davi, eva, fabio, ana, bruno);
                db.SaveChanges();

                var calendario = new CalendarioLetivo
                {
                    TenantId = tenantId, AnoLetivoId = ano.Id, PublicadoEm = DateTime.UtcNow,
                    Eventos =
                    {
                        new EventoCalendario
                        {
                            TenantId = tenantId, Data = feriado, Tipo = EventoCalendario.TipoFeriado, Descricao = "Padroeira", ComAula = false,
                        },
                    },
                };
                var avaliacao = new Avaliacao
                {
                    TenantId = tenantId, TurmaId = turma.Id, DisciplinaId = matematica.Id, PeriodoAvaliativoId = ano.Periodos.First().Id,
                    Nome = "Prova 1", Data = comTudo, RegistradoPorUsuarioId = usuarioCarla.Id,
                    Notas = { new NotaAvaliacao { TenantId = tenantId, AlunoId = ana.Id, Valor = 8m, LancadaPorUsuarioId = usuarioCarla.Id } },
                };
                db.AddRange(calendario, avaliacao);

                for (var dia = HorarioAula.PrimeiroDia; dia <= HorarioAula.UltimoDia; dia++)
                {
                    db.AddRange(
                        new HorarioAula { TenantId = tenantId, TurmaId = turma.Id, DiaSemana = dia, Ordem = 1, DisciplinaId = matematica.Id },
                        new HorarioAula { TenantId = tenantId, TurmaId = turma.Id, DiaSemana = dia, Ordem = 2, DisciplinaId = matematica.Id },
                        new HorarioAula { TenantId = tenantId, TurmaId = turma.Id, DiaSemana = dia, Ordem = 3, DisciplinaId = historia.Id });
                }

                db.AddRange(
                    new ProfessorEscola { TenantId = tenantId, ProfessorId = carla.Id, EscolaId = escola.Id },
                    new ProfessorEscola { TenantId = tenantId, ProfessorId = davi.Id, EscolaId = escola.Id },
                    new ProfessorEscola { TenantId = tenantId, ProfessorId = eva.Id, EscolaId = escola.Id },
                    new ProfessorAlocacao { TenantId = tenantId, ProfessorId = carla.Id, TurmaId = turma.Id, DisciplinaId = matematica.Id },
                    new ProfessorAlocacao { TenantId = tenantId, ProfessorId = davi.Id, TurmaId = turma.Id, DisciplinaId = historia.Id },
                    new ProfessorAlocacao { TenantId = tenantId, ProfessorId = fabio.Id, TurmaId = outraTurma.Id, DisciplinaId = historia.Id },
                    new AlunoTurma { TenantId = tenantId, AlunoId = ana.Id, TurmaId = turma.Id, DataInicio = inicio },
                    new AlunoTurma { TenantId = tenantId, AlunoId = bruno.Id, TurmaId = turma.Id, DataInicio = inicio },
                    NovaChamada(tenantId, turma.Id, matematica.Id, comTudo, usuarioCarla.Id),
                    NovaChamada(tenantId, turma.Id, matematica.Id, soChamada, usuarioCarla.Id),
                    new ConteudoMinistrado
                    {
                        TenantId = tenantId, TurmaId = turma.Id, DisciplinaId = matematica.Id, Data = comTudo,
                        Descricao = "Frações", RegistradoPorUsuarioId = usuarioCarla.Id,
                    },
                    new UsuarioPerfil { UsuarioId = gerencia.Id, PerfilId = perfilGerencia.Id },
                    new UsuarioPerfil { UsuarioId = usuarioCarla.Id, PerfilId = perfilProfessor.Id },
                    new UsuarioPerfil { UsuarioId = usuarioEva.Id, PerfilId = perfilProfessor.Id },
                    new UsuarioPerfil { UsuarioId = usuarioFabio.Id, PerfilId = perfilProfessor.Id },
                    new UsuarioPerfil { UsuarioId = usuarioGil.Id, PerfilId = perfilProfessor.Id });
                db.AddRange(
                    new[] { Permissoes.Horarios.Visualizar, Permissoes.Horarios.Editar }
                        .Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilGerencia.Id, Permissao = p })
                        .Concat(new[] { Permissoes.Horarios.Visualizar, Permissoes.Notas.Visualizar, Permissoes.Chamada.Visualizar }
                            .Select(p => new PerfilPermissao { TenantId = tenantId, PerfilId = perfilProfessor.Id, Permissao = p })));
                db.SaveChanges();

                var cenario = new Cenario(
                    gerencia.Id, usuarioCarla.Id, usuarioEva.Id, usuarioFabio.Id, usuarioGil.Id, matematica.Id, outraTurma.Id, inicio, termino, feriado, comTudo, soChamada);
                PorFactory[factory] = cenario;
                return cenario;
            }
        }

        private static Turma NovaTurma(int tenantId, int anoId, int escolaId, int modalidadeId, int etapaId, string nome) => new()
        {
            TenantId = tenantId, AnoLetivoId = anoId, EscolaId = escolaId, ModalidadeEnsinoId = modalidadeId,
            EtapaEnsinoId = etapaId, NomeIdentificador = nome, NomeCompleto = $"6º Ano {nome}", VagasOfertadas = 30,
        };

        private static Chamada NovaChamada(int tenantId, int turmaId, int disciplinaId, DateOnly data, int usuarioId) => new()
        {
            TenantId = tenantId, TurmaId = turmaId, DisciplinaId = disciplinaId, Data = data, QuantidadeAulas = 2,
            RegistradoPorUsuarioId = usuarioId,
        };

        private static Professor NovoProfessor(int tenantId, string nome, string cpf, int? usuarioId) => new()
        {
            TenantId = tenantId, Nome = nome, Cpf = cpf, Email = $"{cpf}@rede.com", Telefone = "11987654321",
            DataNascimento = new DateTime(1985, 5, 15), UsuarioId = usuarioId,
        };

        private static Aluno NovoAluno(int tenantId, int escolaId, string nome, string matricula) => new()
        {
            TenantId = tenantId, EscolaId = escolaId, Nome = nome, Matricula = matricula, Status = Aluno.StatusAtivo,
            Sexo = Aluno.SexoFeminino, DataNascimento = new DateTime(2014, 3, 10),
            ResponsavelNome1 = "Responsável", ResponsavelTelefone1 = "11987654321",
        };
    }
}
