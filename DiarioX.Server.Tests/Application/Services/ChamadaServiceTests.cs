using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Chamadas;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;
using Xunit;

namespace DiarioX.Server.Tests.Application.Services;

public class ChamadaServiceTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);
    private static readonly UsuarioAtual Gestao = new(UsuarioId: 1, IsGlobalAdmin: false);
    private static readonly UsuarioAtual UsuarioProfessor = new(UsuarioId: 2, IsGlobalAdmin: false);

    [Fact]
    public async Task GetTurmas_Professor_VeApenasSuasAlocacoes()
    {
        var f = new Fixture();

        var turmas = (await f.Service.GetTurmasAsync(UsuarioProfessor)).ToList();

        var turma = Assert.Single(turmas);
        Assert.Equal(Fixture.TurmaId, turma.TurmaId);
        Assert.Equal(["Matemática"], turma.Disciplinas.Select(d => d.Nome));
    }

    [Fact]
    public async Task GetTurmas_Gestao_VeTodasAsDisciplinasDaGrade()
    {
        var f = new Fixture();

        var turma = Assert.Single(await f.Service.GetTurmasAsync(Gestao));

        Assert.Equal(["História", "Matemática"], turma.Disciplinas.Select(d => d.Nome));
    }

    [Fact]
    public async Task Create_ProfessorEmDisciplinaNaoAlocada_RetornaForbidden()
    {
        var f = new Fixture();

        var result = await f.Service.CreateAsync(UsuarioProfessor, f.Request(Fixture.HistoriaId));

        Assert.False(result.Success);
        Assert.Equal(ChamadaResultError.Forbidden, result.Error);
        f.Chamadas.Verify(r => r.AddAsync(It.IsAny<Chamada>()), Times.Never);
    }

    [Fact]
    public async Task Create_ComTodosOsAlunos_GravaRegistros()
    {
        var f = new Fixture();
        Chamada? gravada = null;
        f.Chamadas.Setup(r => r.AddAsync(It.IsAny<Chamada>())).Callback<Chamada>(c => gravada = c).ReturnsAsync((Chamada c) => c);

        var request = f.Request(Fixture.MatematicaId, quantidadeAulas: 2,
            (Fixture.AnaId, "PRESENTE", null), (Fixture.BrunoId, "falta_justificada", "Atestado médico"));
        request.Conteudo = "  Frações  ";

        var result = await f.Service.CreateAsync(UsuarioProfessor, request);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(gravada);
        Assert.Equal(2, gravada!.QuantidadeAulas);
        Assert.Equal("Frações", gravada.Conteudo);
        Assert.Equal(UsuarioProfessor.UsuarioId, gravada.RegistradoPorUsuarioId);
        var bruno = gravada.Registros.Single(r => r.AlunoId == Fixture.BrunoId);
        Assert.Equal(ChamadaAluno.SituacaoFaltaJustificada, bruno.Situacao);
        Assert.Equal("Atestado médico", bruno.Justificativa);
    }

    [Fact]
    public async Task Create_SemInformarTodosOsAlunos_ListaPendentes()
    {
        var f = new Fixture();

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId, 1, (Fixture.AnaId, "PRESENTE", null)));

        Assert.False(result.Success);
        Assert.Contains("Bruno Lima", result.Message);
    }

    [Fact]
    public async Task Create_FaltaJustificadaSemMotivo_RetornaErro()
    {
        var f = new Fixture();

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId, 1,
            (Fixture.AnaId, "PRESENTE", null), (Fixture.BrunoId, "FALTA_JUSTIFICADA", " ")));

        Assert.False(result.Success);
        Assert.Equal("Informe a justificativa da falta de Bruno Lima.", result.Message);
    }

    [Fact]
    public async Task Create_AlunoForaDaTurma_RetornaErro()
    {
        var f = new Fixture();

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId, 1,
            (Fixture.AnaId, "PRESENTE", null), (Fixture.BrunoId, "PRESENTE", null), (999, "PRESENTE", null)));

        Assert.False(result.Success);
        Assert.Contains("não está enturmado", result.Message);
    }

    [Fact]
    public async Task Create_DataFutura_RetornaErro()
    {
        var f = new Fixture();
        var request = f.Request(Fixture.MatematicaId, 1, (Fixture.AnaId, "PRESENTE", null), (Fixture.BrunoId, "PRESENTE", null));
        request.Data = Hoje.AddDays(1);

        var result = await f.Service.CreateAsync(Gestao, request);

        Assert.False(result.Success);
        Assert.Equal("A data da chamada não pode ser futura.", result.Message);
    }

    [Fact]
    public async Task Create_QuandoJaExisteChamadaNaData_RetornaConflito()
    {
        var f = new Fixture();
        f.Chamadas.Setup(r => r.GetAsync(Fixture.TurmaId, Fixture.MatematicaId, Hoje))
            .ReturnsAsync(new Chamada { Id = 5, TurmaId = Fixture.TurmaId, DisciplinaId = Fixture.MatematicaId, Data = Hoje });

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId, 1,
            (Fixture.AnaId, "PRESENTE", null), (Fixture.BrunoId, "PRESENTE", null)));

        Assert.Equal(ChamadaResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task Frequencia_ContaAulasEJustificadasComoAusencia()
    {
        var f = new Fixture();
        var ana = f.Alunos[Fixture.AnaId];
        var bruno = f.Alunos[Fixture.BrunoId];
        Chamada Aula(int dias, int aulas, string situacaoBruno) => new()
        {
            TurmaId = Fixture.TurmaId, DisciplinaId = Fixture.MatematicaId, Data = Hoje.AddDays(-dias), QuantidadeAulas = aulas,
            Registros =
            [
                new ChamadaAluno { AlunoId = ana.Id, Aluno = ana, Situacao = ChamadaAluno.SituacaoPresente },
                new ChamadaAluno { AlunoId = bruno.Id, Aluno = bruno, Situacao = situacaoBruno },
            ],
        };
        f.Chamadas.Setup(r => r.ListAsync(Fixture.TurmaId, Fixture.MatematicaId, It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync([
                Aula(3, 2, ChamadaAluno.SituacaoFalta),
                Aula(2, 1, ChamadaAluno.SituacaoFaltaJustificada),
                Aula(1, 1, ChamadaAluno.SituacaoPresente),
            ]);

        var result = await f.Service.GetFrequenciaAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, periodoId: null);

        Assert.True(result.Success);
        Assert.Equal(4, result.Value!.AulasDadas);
        var linhaAna = result.Value.Alunos.Single(a => a.AlunoId == Fixture.AnaId);
        var linhaBruno = result.Value.Alunos.Single(a => a.AlunoId == Fixture.BrunoId);
        Assert.Equal(100m, linhaAna.PercentualFrequencia);
        Assert.False(linhaAna.AbaixoDoMinimo);
        Assert.Equal((4, 2, 1), (linhaBruno.Aulas, linhaBruno.Faltas, linhaBruno.FaltasJustificadas));
        Assert.Equal(25m, linhaBruno.PercentualFrequencia);
        Assert.True(linhaBruno.AbaixoDoMinimo);
    }

    /// <summary>
    /// Turma "6º Ano A" com Ana e Bruno enturmados; grade com Matemática e História. O usuário 2 é
    /// professor alocado só em Matemática; o usuário 1 é da gestão (não é professor).
    /// </summary>
    private sealed class Fixture
    {
        public const int TurmaId = 10, MatematicaId = 20, HistoriaId = 21, AnaId = 100, BrunoId = 101, EtapaId = 5;

        public Mock<IChamadaRepository> Chamadas { get; } = new();
        public Dictionary<int, Aluno> Alunos { get; }
        public ChamadaService Service { get; }

        public Fixture()
        {
            var anoLetivo = new AnoLetivo
            {
                Id = 1, AnoReferencia = Hoje.Year,
                DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(60),
            };
            var turma = new Turma
            {
                Id = TurmaId, NomeCompleto = "6º Ano A", EtapaEnsinoId = EtapaId, Status = Turma.StatusAtivo,
                AnoLetivoId = 1, AnoLetivo = anoLetivo, Escola = new Escola { Nome = "Escola A" },
            };
            var matematica = Disciplina(MatematicaId, "Matemática");
            var historia = Disciplina(HistoriaId, "História");

            Alunos = new Dictionary<int, Aluno>
            {
                [AnaId] = new() { Id = AnaId, Nome = "Ana Souza", Matricula = "20260001", Status = Aluno.StatusAtivo },
                [BrunoId] = new() { Id = BrunoId, Nome = "Bruno Lima", Matricula = "20260002", Status = Aluno.StatusAtivo },
            };

            var turmas = new Mock<ITurmaRepository>();
            turmas.Setup(r => r.GetByIdAsync(TurmaId)).ReturnsAsync(turma);
            turmas.Setup(r => r.GetAllAsync()).ReturnsAsync([turma]);

            var disciplinas = new Mock<IDisciplinaRepository>();
            disciplinas.Setup(r => r.GetByIdAsync(MatematicaId)).ReturnsAsync(matematica);
            disciplinas.Setup(r => r.GetByIdAsync(HistoriaId)).ReturnsAsync(historia);
            disciplinas.Setup(r => r.GetAllAsync()).ReturnsAsync([matematica, historia]);

            var anos = new Mock<IAnoLetivoRepository>();
            anos.Setup(r => r.GetAllAsync()).ReturnsAsync([anoLetivo]);

            var professores = new Mock<IProfessorRepository>();
            professores.Setup(r => r.GetByUsuarioIdAsync(UsuarioProfessor.UsuarioId)).ReturnsAsync(new Professor { Id = 7 });

            var alocacoes = new Mock<IProfessorAlocacaoRepository>();
            alocacoes.Setup(r => r.GetByProfessorIdAsync(7))
                .ReturnsAsync([new ProfessorAlocacao { ProfessorId = 7, TurmaId = TurmaId, DisciplinaId = MatematicaId }]);

            Chamadas.Setup(r => r.GetEnturmacoesAsync(TurmaId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(Alunos.Values.Select(a => new AlunoTurma { AlunoId = a.Id, Aluno = a, TurmaId = TurmaId }).ToList());
            Chamadas.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
                .ReturnsAsync([]);
            Chamadas.Setup(r => r.GetEmailsUsuariosAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, string>());

            Service = new ChamadaService(Chamadas.Object, turmas.Object, disciplinas.Object, anos.Object,
                professores.Object, alocacoes.Object);
        }

        public ChamadaRequest Request(int disciplinaId, int quantidadeAulas = 1, params (int AlunoId, string Situacao, string? Justificativa)[] alunos)
        {
            // Sem alunos informados: todos presentes.
            var itens = alunos.Length > 0 ? alunos : Alunos.Keys.Select(id => (id, "PRESENTE", (string?)null)).ToArray();
            return new ChamadaRequest
            {
                TurmaId = TurmaId, DisciplinaId = disciplinaId, Data = Hoje, QuantidadeAulas = quantidadeAulas,
                Alunos = itens.Select(a => new ChamadaAlunoRequest { AlunoId = a.Item1, Situacao = a.Item2, Justificativa = a.Item3 }).ToList(),
            };
        }

        private static Disciplina Disciplina(int id, string nome) => new()
        {
            Id = id, Nome = nome, Ativa = true,
            EtapasEnsino = [new DisciplinaEtapaEnsino { DisciplinaId = id, EtapaEnsinoId = EtapaId }],
        };
    }
}
