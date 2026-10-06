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
    private static readonly UsuarioAtual OutroProfessor = new(UsuarioId: 3, IsGlobalAdmin: false);

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
    public async Task Create_DiaSemAulaNoCalendarioPublicado_BloqueiaLancamento()
    {
        var f = new Fixture();
        var calendario = new CalendarioLetivo { Id = 1, PublicadoEm = DateTime.UtcNow };
        calendario.Eventos.Add(new EventoCalendario
        {
            CalendarioLetivoId = 1, Data = Hoje, Tipo = EventoCalendario.TipoConselhoClasse,
            Descricao = "Conselho de Classe", ComAula = false,
        });
        f.Calendarios.Setup(r => r.GetPublicadosAsync(1, It.IsAny<int>())).ReturnsAsync([calendario]);
        const string mensagem =
            "Não é possível registrar frequência. Data configurada como Conselho de Classe no Calendário Escolar.";

        var aula = await f.Service.GetAsync(UsuarioProfessor, Fixture.TurmaId, Fixture.MatematicaId, Hoje);
        var result = await f.Service.CreateAsync(UsuarioProfessor, f.Request(Fixture.MatematicaId));

        Assert.Equal(mensagem, aula.Value!.Bloqueio);
        Assert.False(result.Success);
        Assert.Equal(mensagem, result.Message);
        f.Chamadas.Verify(r => r.AddAsync(It.IsAny<Chamada>()), Times.Never);
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

    [Fact]
    public async Task Create_AlunoTransferido_NaoRecebeNovoLancamento()
    {
        var f = new Fixture();
        f.Alunos[Fixture.BrunoId].Status = Aluno.StatusTransferido;
        Chamada? gravada = null;
        f.Chamadas.Setup(r => r.AddAsync(It.IsAny<Chamada>())).Callback<Chamada>(c => gravada = c).ReturnsAsync((Chamada c) => c);

        var semBruno = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId, 1, (Fixture.AnaId, "PRESENTE", null)));
        Assert.True(semBruno.Success, semBruno.Message);
        Assert.Equal([Fixture.AnaId], gravada!.Registros.Select(r => r.AlunoId));

        var comBruno = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId, 1,
            (Fixture.AnaId, "PRESENTE", null), (Fixture.BrunoId, "PRESENTE", null)));
        Assert.False(comBruno.Success);
        Assert.Contains("não está enturmado", comBruno.Message);
    }

    [Fact]
    public async Task Update_AlunoTransferido_RegistroFicaCongelado()
    {
        var f = new Fixture();
        var bruno = f.Alunos[Fixture.BrunoId];
        bruno.Status = Aluno.StatusTransferido;
        var chamada = new Chamada
        {
            Id = 5, TurmaId = Fixture.TurmaId, DisciplinaId = Fixture.MatematicaId, Data = Hoje,
            Registros =
            [
                new ChamadaAluno { AlunoId = Fixture.AnaId, Aluno = f.Alunos[Fixture.AnaId], Situacao = ChamadaAluno.SituacaoPresente },
                new ChamadaAluno { AlunoId = Fixture.BrunoId, Aluno = bruno, Situacao = ChamadaAluno.SituacaoFalta },
            ],
        };
        f.Chamadas.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(chamada);
        IEnumerable<ChamadaAluno>? gravados = null;
        f.Chamadas.Setup(r => r.UpdateAsync(chamada, It.IsAny<IEnumerable<ChamadaAluno>>()))
            .Callback<Chamada, IEnumerable<ChamadaAluno>>((_, r) => gravados = r.ToList());

        var alterandoBruno = await f.Service.UpdateAsync(Gestao, 5, f.Request(Fixture.MatematicaId, 1,
            (Fixture.AnaId, "PRESENTE", null), (Fixture.BrunoId, "PRESENTE", null)));
        Assert.False(alterandoBruno.Success);
        Assert.Equal("Bruno Lima foi transferido(a): a frequência registrada não pode ser alterada.", alterandoBruno.Message);

        var alterandoAna = await f.Service.UpdateAsync(Gestao, 5, f.Request(Fixture.MatematicaId, 1,
            (Fixture.AnaId, "FALTA", null), (Fixture.BrunoId, "FALTA", null)));
        Assert.True(alterandoAna.Success, alterandoAna.Message);
        Assert.Equal(ChamadaAluno.SituacaoFalta, gravados!.Single(r => r.AlunoId == Fixture.AnaId).Situacao);
        Assert.Equal(ChamadaAluno.SituacaoFalta, gravados!.Single(r => r.AlunoId == Fixture.BrunoId).Situacao);
    }

    // ---------- RF017 RN02: Anos Iniciais (frequência diária) x Anos Finais (por aula) ----------

    [Fact]
    public async Task Create_FrequenciaDiaria_GravaUmaChamadaPorDiaSemDisciplina()
    {
        var f = new Fixture(EtapaEnsino.FrequenciaDiaria);
        Chamada? gravada = null;
        f.Chamadas.Setup(r => r.AddAsync(It.IsAny<Chamada>())).Callback<Chamada>(c => gravada = c).ReturnsAsync((Chamada c) => c);
        var request = f.Request(Fixture.MatematicaId, quantidadeAulas: 3);
        request.DisciplinaId = null;

        var result = await f.Service.CreateAsync(UsuarioProfessor, request);

        Assert.True(result.Success, result.Message);
        Assert.Null(gravada!.DisciplinaId);
        Assert.Equal(1, gravada.QuantidadeAulas);
    }

    [Fact]
    public async Task Create_FrequenciaDiaria_IgnoraDisciplinaInformada()
    {
        var f = new Fixture(EtapaEnsino.FrequenciaDiaria);
        Chamada? gravada = null;
        f.Chamadas.Setup(r => r.AddAsync(It.IsAny<Chamada>())).Callback<Chamada>(c => gravada = c).ReturnsAsync((Chamada c) => c);

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.HistoriaId));

        Assert.True(result.Success, result.Message);
        Assert.Null(gravada!.DisciplinaId);
    }

    [Fact]
    public async Task Create_FrequenciaDiaria_JaRegistradaNoDia_RetornaConflito()
    {
        var f = new Fixture(EtapaEnsino.FrequenciaDiaria);
        f.Chamadas.Setup(r => r.GetAsync(Fixture.TurmaId, null, Hoje))
            .ReturnsAsync(new Chamada { Id = 5, TurmaId = Fixture.TurmaId, Data = Hoje });

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId));

        Assert.Equal(ChamadaResultError.Conflict, result.Error);
        Assert.Equal("Já existe uma chamada registrada para esta turma e data. Abra a chamada existente para alterá-la.", result.Message);
    }

    [Fact]
    public async Task GetTurmas_FrequenciaDiaria_NaoListaDisciplinas_ESoParaProfessorDaTurma()
    {
        var f = new Fixture(EtapaEnsino.FrequenciaDiaria);

        var daGestao = Assert.Single(await f.Service.GetTurmasAsync(Gestao));
        var doProfessor = Assert.Single(await f.Service.GetTurmasAsync(UsuarioProfessor));
        var deOutroProfessor = await f.Service.GetTurmasAsync(OutroProfessor);

        Assert.Equal(EtapaEnsino.FrequenciaDiaria, daGestao.TipoFrequencia);
        Assert.Empty(daGestao.Disciplinas);
        Assert.Empty(doProfessor.Disciplinas);
        Assert.Empty(deOutroProfessor);
    }

    [Fact]
    public async Task Create_FrequenciaDiaria_ProfessorForaDaTurma_RetornaForbidden()
    {
        var f = new Fixture(EtapaEnsino.FrequenciaDiaria);

        var result = await f.Service.CreateAsync(OutroProfessor, f.Request(Fixture.MatematicaId));

        Assert.Equal(ChamadaResultError.Forbidden, result.Error);
        f.Chamadas.Verify(r => r.AddAsync(It.IsAny<Chamada>()), Times.Never);
    }

    [Fact]
    public async Task Create_FrequenciaPorAula_SemDisciplina_RetornaErro()
    {
        var f = new Fixture();
        var request = f.Request(Fixture.MatematicaId);
        request.DisciplinaId = null;

        var result = await f.Service.CreateAsync(Gestao, request);

        Assert.Equal(ChamadaResultError.Validation, result.Error);
        Assert.Equal("Selecione a disciplina da chamada.", result.Message);
    }

    [Fact]
    public async Task GetTurmas_FrequenciaPorAula_InformaOTipo()
    {
        var f = new Fixture();

        var turma = Assert.Single(await f.Service.GetTurmasAsync(Gestao));

        Assert.Equal(EtapaEnsino.FrequenciaPorAula, turma.TipoFrequencia);
    }

    // ---------- RF017 EX01: data sem aula ----------

    [Fact]
    public async Task Get_DataSemAulaSemChamada_NaoAbreAListaDeAlunos()
    {
        var f = new Fixture();
        f.PublicarEventoSemAula(Hoje, "Recesso Escolar");

        var aula = await f.Service.GetAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Hoje);

        Assert.Equal("Não é possível registrar frequência. Data configurada como Recesso Escolar no Calendário Escolar.", aula.Value!.Bloqueio);
        Assert.Empty(aula.Value.Alunos);
    }

    [Fact]
    public async Task Get_FimDeSemanaComCalendarioPublicado_BloqueiaComOTextoDaFrequencia()
    {
        var f = new Fixture();
        var sabado = Hoje;
        while (sabado.DayOfWeek != DayOfWeek.Saturday) sabado = sabado.AddDays(-1);
        f.Calendarios.Setup(r => r.GetPublicadosAsync(1, It.IsAny<int>()))
            .ReturnsAsync([new CalendarioLetivo { Id = 1, PublicadoEm = DateTime.UtcNow }]);
        f.AnoLetivo.DataInicio = sabado.AddDays(-30);

        var aula = await f.Service.GetAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, sabado);

        Assert.Equal(
            "Não é possível registrar frequência. Data configurada como sábado sem Dia Letivo Especial (Sábado Letivo) no Calendário Escolar.",
            aula.Value!.Bloqueio);
    }

    // ---------- RF017 EX02: período encerrado ----------

    private const string MensagemPeriodoEncerrado = "Este período letivo está encerrado para alterações. Contate a coordenação pedagógica.";

    [Fact]
    public async Task Create_PeriodoEncerrado_BloqueiaERecomendaACoordenacao()
    {
        var f = new Fixture();
        f.EncerrarPeriodoQueContem(Hoje);

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId));

        Assert.False(result.Success);
        Assert.Equal(ChamadaResultError.Validation, result.Error);
        Assert.Equal(MensagemPeriodoEncerrado, result.Message);
        f.Chamadas.Verify(r => r.AddAsync(It.IsAny<Chamada>()), Times.Never);
    }

    [Fact]
    public async Task Update_PeriodoEncerrado_BloqueiaAAlteracao()
    {
        var f = new Fixture();
        f.EncerrarPeriodoQueContem(Hoje);
        var chamada = new Chamada { Id = 5, TurmaId = Fixture.TurmaId, DisciplinaId = Fixture.MatematicaId, Data = Hoje };
        f.Chamadas.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(chamada);

        var result = await f.Service.UpdateAsync(Gestao, 5, f.Request(Fixture.MatematicaId));

        Assert.Equal(MensagemPeriodoEncerrado, result.Message);
        f.Chamadas.Verify(r => r.UpdateAsync(It.IsAny<Chamada>(), It.IsAny<IEnumerable<ChamadaAluno>>()), Times.Never);
    }

    [Fact]
    public async Task Delete_PeriodoEncerrado_BloqueiaAExclusao()
    {
        var f = new Fixture();
        f.EncerrarPeriodoQueContem(Hoje);
        var chamada = new Chamada { Id = 5, TurmaId = Fixture.TurmaId, DisciplinaId = Fixture.MatematicaId, Data = Hoje };
        f.Chamadas.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(chamada);

        var result = await f.Service.DeleteAsync(Gestao, 5);

        Assert.Equal(MensagemPeriodoEncerrado, result.Message);
        f.Chamadas.Verify(r => r.DeleteAsync(It.IsAny<Chamada>()), Times.Never);
    }

    [Fact]
    public async Task Get_PeriodoEncerrado_AbreSoParaConsulta()
    {
        var f = new Fixture();
        f.EncerrarPeriodoQueContem(Hoje);

        var aula = await f.Service.GetAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Hoje);

        Assert.True(aula.Value!.PeriodoEncerrado);
        Assert.NotEmpty(aula.Value.Alunos);
    }

    [Fact]
    public async Task Create_PeriodoEncerradoNaoContemAData_PermiteOLancamento()
    {
        var f = new Fixture();
        f.EncerrarPeriodoQueContem(Hoje.AddDays(-40));
        f.Chamadas.Setup(r => r.AddAsync(It.IsAny<Chamada>())).ReturnsAsync((Chamada c) => c);

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId));

        Assert.True(result.Success, result.Message);
    }

    /// <summary>
    /// Turma "6º Ano A" com Ana e Bruno enturmados; grade com Matemática e História. O usuário 2 é
    /// professor alocado só em Matemática; o usuário 1 é da gestão (não é professor).
    /// </summary>
    private sealed class Fixture
    {
        public const int TurmaId = 10, MatematicaId = 20, HistoriaId = 21, AnaId = 100, BrunoId = 101, EtapaId = 5;

        public Mock<IChamadaRepository> Chamadas { get; } = new();
        public Mock<ICalendarioLetivoRepository> Calendarios { get; } = new();
        public Dictionary<int, Aluno> Alunos { get; }
        public ChamadaService Service { get; }

        public AnoLetivo AnoLetivo { get; }

        public Fixture(string tipoFrequencia = EtapaEnsino.FrequenciaPorAula)
        {
            var anoLetivo = new AnoLetivo
            {
                Id = 1, AnoReferencia = Hoje.Year,
                DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(60),
            };
            AnoLetivo = anoLetivo;
            var turma = new Turma
            {
                Id = TurmaId, NomeCompleto = "6º Ano A", EtapaEnsinoId = EtapaId, Status = Turma.StatusAtivo,
                EtapaEnsino = new EtapaEnsino { Id = EtapaId, TipoFrequencia = tipoFrequencia },
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
            anos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(anoLetivo);

            var professores = new Mock<IProfessorRepository>();
            professores.Setup(r => r.GetByUsuarioIdAsync(UsuarioProfessor.UsuarioId)).ReturnsAsync(new Professor { Id = 7 });
            professores.Setup(r => r.GetByUsuarioIdAsync(OutroProfessor.UsuarioId)).ReturnsAsync(new Professor { Id = 8 });

            var alocacoes = new Mock<IProfessorAlocacaoRepository>();
            alocacoes.Setup(r => r.GetByProfessorIdAsync(8)).ReturnsAsync([]);
            alocacoes.Setup(r => r.GetByProfessorIdAsync(7))
                .ReturnsAsync([new ProfessorAlocacao { ProfessorId = 7, TurmaId = TurmaId, DisciplinaId = MatematicaId }]);

            Chamadas.Setup(r => r.GetEnturmacoesAsync(TurmaId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(Alunos.Values.Select(a => new AlunoTurma { AlunoId = a.Id, Aluno = a, TurmaId = TurmaId }).ToList());
            Chamadas.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
                .ReturnsAsync([]);
            Chamadas.Setup(r => r.GetEmailsUsuariosAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, string>());

            Calendarios.Setup(r => r.GetPublicadosAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync([]);

            Service = new ChamadaService(Chamadas.Object, turmas.Object, disciplinas.Object, anos.Object,
                professores.Object, alocacoes.Object, Calendarios.Object);
        }

        /// <summary>Publica um calendário com o dia marcado como sem aula (feriado, recesso, conselho).</summary>
        public void PublicarEventoSemAula(DateOnly data, string descricao)
        {
            var calendario = new CalendarioLetivo { Id = 1, PublicadoEm = DateTime.UtcNow };
            calendario.Eventos.Add(new EventoCalendario
            {
                CalendarioLetivoId = 1, Data = data, Tipo = EventoCalendario.TipoConselhoClasse,
                Descricao = descricao, ComAula = false,
            });
            Calendarios.Setup(r => r.GetPublicadosAsync(1, It.IsAny<int>())).ReturnsAsync([calendario]);
        }

        /// <summary>Marca como encerrado o período avaliativo (de 20 dias para cada lado) que contém a data.</summary>
        public void EncerrarPeriodoQueContem(DateOnly data)
            => AnoLetivo.Periodos.Add(new PeriodoAvaliativo
            {
                AnoLetivoId = AnoLetivo.Id, Nome = "1º Bimestre", Numero = 1, Encerrado = true,
                DataInicio = data.AddDays(-20), DataTermino = data.AddDays(20),
            });

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
