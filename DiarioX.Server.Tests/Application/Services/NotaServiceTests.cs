using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Notas;
using DiarioX.Server.Application.Notas;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;
using Xunit;

namespace DiarioX.Server.Tests.Application.Services;

public class NotaServiceTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);
    private static readonly UsuarioAtual Gestao = new(UsuarioId: 1, IsGlobalAdmin: false);
    private static readonly UsuarioAtual UsuarioProfessor = new(UsuarioId: 2, IsGlobalAdmin: false);

    [Fact]
    public async Task GetTurmas_Professor_VeApenasSuasAlocacoes()
    {
        var f = new Fixture();

        var turma = Assert.Single(await f.Service.GetTurmasAsync(UsuarioProfessor));

        Assert.Equal(["Matemática"], turma.Disciplinas.Select(d => d.Nome));
        Assert.Equal(2, turma.Periodos.Count);
    }

    [Fact]
    public async Task GetTurmas_EtapaSemRegra_UsaAPadraoDoSistema()
    {
        var f = new Fixture();

        var turma = Assert.Single(await f.Service.GetTurmasAsync(Gestao));

        Assert.Null(turma.Regra.Id);
        Assert.Equal((10m, 6m), (turma.Regra.NotaMaxima, turma.Regra.MediaAprovacao));
    }

    [Fact]
    public async Task GetTurmas_EtapaComRegra_UsaARegraDaEtapa()
    {
        var f = new Fixture();
        var regra = new RegraAvaliacao { Id = 3, Nome = "Média 7", NotaMaxima = 10m, MediaAprovacao = 7m, Etapas = [new EtapaEnsino { Id = Fixture.EtapaId }] };
        f.Regras.Setup(r => r.GetAllAsync()).ReturnsAsync([regra]);

        var turma = Assert.Single(await f.Service.GetTurmasAsync(Gestao));

        Assert.Equal((3, 7m), (turma.Regra.Id, turma.Regra.MediaAprovacao));
    }

    [Fact]
    public async Task SalvarAvaliacao_ProfessorEmDisciplinaNaoAlocada_RetornaForbidden()
    {
        var f = new Fixture();

        var result = await f.Service.SalvarAvaliacaoAsync(UsuarioProfessor, null, f.Requisicao(Fixture.HistoriaId));

        Assert.Equal(NotaResultError.Forbidden, result.Error);
        f.Avaliacoes.Verify(r => r.AddAsync(It.IsAny<Avaliacao>()), Times.Never);
    }

    [Fact]
    public async Task SalvarAvaliacao_Valida_GravaComOUsuario()
    {
        var f = new Fixture();
        Avaliacao? gravada = null;
        f.Avaliacoes.Setup(r => r.AddAsync(It.IsAny<Avaliacao>())).Callback<Avaliacao>(a => gravada = a).ReturnsAsync((Avaliacao a) => a);

        var request = f.Requisicao(Fixture.MatematicaId);
        request.Nome = "  Prova 1  ";
        request.Tipo = "prova";
        request.Peso = 2m;

        var result = await f.Service.SalvarAvaliacaoAsync(UsuarioProfessor, null, request);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(gravada);
        Assert.Equal(("Prova 1", Avaliacao.TipoProva, 2m), (gravada!.Nome, gravada.Tipo, gravada.Peso));
        Assert.Equal(UsuarioProfessor.UsuarioId, gravada.RegistradoPorUsuarioId);
        Assert.Null(gravada.ValorMaximo);
    }

    [Fact]
    public async Task SalvarAvaliacao_DataForaDoPeriodo_RetornaErro()
    {
        var f = new Fixture();
        var request = f.Requisicao(Fixture.MatematicaId);
        request.Data = Hoje.AddDays(10);

        var result = await f.Service.SalvarAvaliacaoAsync(Gestao, null, request);

        Assert.Equal(NotaResultError.Validation, result.Error);
        Assert.Contains("dentro do período 1º Bimestre", result.Message);
    }

    [Fact]
    public async Task SalvarAvaliacao_SegundaRecuperacaoNoPeriodo_RetornaConflito()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, tipo: Avaliacao.TipoRecuperacao));
        var request = f.Requisicao(Fixture.MatematicaId);
        request.Tipo = Avaliacao.TipoRecuperacao;

        var result = await f.Service.SalvarAvaliacaoAsync(Gestao, null, request);

        Assert.Equal(NotaResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task SalvarAvaliacao_Soma_PontosAcimaDaNotaMaxima_RetornaErro()
    {
        var f = new Fixture();
        f.UsarRegra(new RegraAvaliacao { Id = 4, Nome = "Soma", NotaMaxima = 10m, MediaAprovacao = 6m, CalculoNotaPeriodo = RegraAvaliacao.CalculoSoma });
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, valorMaximo: 7m));
        var request = f.Requisicao(Fixture.MatematicaId);
        request.ValorMaximo = 4m;

        var result = await f.Service.SalvarAvaliacaoAsync(Gestao, null, request);

        Assert.Equal(NotaResultError.Validation, result.Error);
        Assert.Contains("ficaria em 11", result.Message);
    }

    [Fact]
    public async Task SalvarAvaliacao_TurmaInativa_RetornaErro()
    {
        var f = new Fixture();
        f.Turma.Status = Turma.StatusInativo;

        var result = await f.Service.SalvarAvaliacaoAsync(Gestao, null, f.Requisicao(Fixture.MatematicaId));

        Assert.Equal(NotaResultError.Validation, result.Error);
    }

    [Fact]
    public async Task LancarNotas_ValorAcimaDaNotaMaxima_RetornaErroDeExcesso()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, nome: "Prova 1"));

        var result = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.AnaId, 10.5m)));

        Assert.Equal(NotaResultError.Validation, result.Error);
        Assert.Equal("A nota informada excede o valor máximo permitido (10.0).", result.Message);
        f.Avaliacoes.Verify(r => r.SalvarNotasAsync(It.IsAny<IReadOnlyCollection<NotaInformada>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LancarNotas_NotaNegativa_RetornaErro()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId));

        var result = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.AnaId, -0.5m)));

        Assert.Equal("A nota informada não pode ser menor que zero.", result.Message);
        f.Avaliacoes.Verify(r => r.SalvarNotasAsync(It.IsAny<IReadOnlyCollection<NotaInformada>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LancarNotas_NaRegraDeSoma_ExcessoUsaOValorDaAvaliacao()
    {
        var f = new Fixture();
        f.UsarRegra(new RegraAvaliacao { NotaMaxima = 10m, MediaAprovacao = 6m, CalculoNotaPeriodo = RegraAvaliacao.CalculoSoma });
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, valorMaximo: 4m));

        var result = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.AnaId, 4.5m)));

        Assert.Equal("A nota informada excede o valor máximo permitido (4.0).", result.Message);
    }

    // ---------- EX02: prazo de lançamento ----------

    [Fact]
    public async Task PrazoExpirado_BloqueiaLancamentoEAvaliacoes_ComMensagemDoPrazo()
    {
        var f = new Fixture();
        var prazo = Hoje.AddDays(-3);
        f.PrazoDoPeriodo1(prazo);
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId));
        var mensagem = $"Lançamento de notas bloqueado para este bimestre. Prazo encerrado em {prazo:dd/MM/yyyy}.";
        f.Avaliacoes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(f.Lista[0]);

        var lancamento = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.AnaId, 7m)));
        var criacao = await f.Service.SalvarAvaliacaoAsync(Gestao, null, f.Requisicao(Fixture.MatematicaId));
        var exclusao = await f.Service.ExcluirAvaliacaoAsync(Gestao, 1);
        var periodo = await f.Service.GetPeriodoAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Fixture.Periodo1Id);

        Assert.Equal(mensagem, lancamento.Message);
        Assert.Equal(mensagem, criacao.Message);
        Assert.Equal(mensagem, exclusao.Message);
        Assert.Equal(mensagem, periodo.Value!.Bloqueio);
        Assert.Equal(prazo, periodo.Value.Periodo.PrazoLancamento);
        f.Avaliacoes.Verify(r => r.SalvarNotasAsync(It.IsAny<IReadOnlyCollection<NotaInformada>>(), It.IsAny<int>()), Times.Never);
        f.Avaliacoes.Verify(r => r.AddAsync(It.IsAny<Avaliacao>()), Times.Never);
        f.Avaliacoes.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PrazoNoDiaDeHojeOuSemPrazo_PermiteLancamento()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId));
        f.Avaliacoes.Setup(r => r.SalvarNotasAsync(It.IsAny<IReadOnlyCollection<NotaInformada>>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var semPrazo = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.AnaId, 7m)));
        f.PrazoDoPeriodo1(Hoje);
        var noUltimoDia = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.AnaId, 7m)));

        Assert.True(semPrazo.Success, semPrazo.Message);
        Assert.True(noUltimoDia.Success, noUltimoDia.Message);
    }

    // ---------- RN03: alunos que saíram da turma ----------

    [Theory]
    [InlineData(AlunoTurma.MotivoTransferencia, "TRANSFERIDO")]
    [InlineData(AlunoTurma.MotivoRemanejamento, "REMANEJADO")]
    [InlineData(AlunoTurma.MotivoOutros, "DESENTURMADO")]
    public async Task GetPeriodo_AlunoQueSaiuDaTurma_MostraNotasSalvasSomenteLeituraComStatus(string motivo, string status)
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, notas: [(Fixture.AnaId, 8m), (Fixture.BrunoId, 6m)]));
        f.Saidas[Fixture.BrunoId] = (Hoje.AddDays(-10), motivo);

        var result = await f.Service.GetPeriodoAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Fixture.Periodo1Id);

        var bruno = result.Value!.Alunos.Single(a => a.AlunoId == Fixture.BrunoId);
        var ana = result.Value.Alunos.Single(a => a.AlunoId == Fixture.AnaId);
        Assert.Equal(status, bruno.StatusSaida);
        Assert.True(bruno.SomenteLeitura);
        Assert.Equal(6m, Assert.Single(bruno.Notas).Valor);
        Assert.Null(ana.StatusSaida);
        Assert.False(ana.SomenteLeitura);
    }

    [Fact]
    public async Task GetPeriodo_AlunoComStatusTransferido_IndicaTransferidoMesmoSemMotivoNoVinculo()
    {
        var f = new Fixture();
        f.Alunos[Fixture.BrunoId].Status = Aluno.StatusTransferido;
        f.Saidas[Fixture.BrunoId] = (Hoje.AddDays(-10), null);

        var result = await f.Service.GetPeriodoAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Fixture.Periodo1Id);

        Assert.Equal("TRANSFERIDO", result.Value!.Alunos.Single(a => a.AlunoId == Fixture.BrunoId).StatusSaida);
    }

    [Fact]
    public async Task LancarNotas_AlterarNotaDeAlunoQueSaiuDaTurma_RetornaErro()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, notas: [(Fixture.BrunoId, 6m)]));
        f.Saidas[Fixture.BrunoId] = (Hoje.AddDays(-10), AlunoTurma.MotivoTransferencia);

        var alteracao = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.BrunoId, 9m)));
        var exclusao = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.BrunoId, null)));

        Assert.Equal("Bruno Lima deixou a turma (Transferido): as notas lançadas ficam somente leitura.", alteracao.Message);
        Assert.Equal(NotaResultError.Validation, exclusao.Error);
        f.Avaliacoes.Verify(r => r.SalvarNotasAsync(It.IsAny<IReadOnlyCollection<NotaInformada>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LancarNotas_EnviandoANotaSalvaDoAlunoQueSaiu_NaoAltera()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, notas: [(Fixture.BrunoId, 6m)]));
        f.Saidas[Fixture.BrunoId] = (Hoje.AddDays(-10), AlunoTurma.MotivoRemanejamento);
        f.Avaliacoes.Setup(r => r.SalvarNotasAsync(It.IsAny<IReadOnlyCollection<NotaInformada>>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.BrunoId, 6m), (1, Fixture.AnaId, 7m)));

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task GetPeriodo_RecuperacaoSuperior_AplicaARegraDaRede()
    {
        var f = new Fixture();
        f.UsarRegra(new RegraAvaliacao
        {
            NotaMaxima = 10m, MediaAprovacao = 6m, CasasDecimais = 1, PermiteRecuperacao = true,
            CalculoNotaPeriodo = RegraAvaliacao.CalculoMediaPonderada, SubstituicaoRecuperacao = RegraAvaliacao.MediaComRecuperacao,
        });
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, notas: [(Fixture.AnaId, 4m)]));
        f.Lista.Add(f.NovaAvaliacao(2, Fixture.MatematicaId, tipo: Avaliacao.TipoRecuperacao, notas: [(Fixture.AnaId, 8m)]));

        var result = await f.Service.GetPeriodoAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Fixture.Periodo1Id);

        var ana = result.Value!.Alunos.Single(a => a.AlunoId == Fixture.AnaId);
        Assert.Equal((4m, 8m, 6m, true), (ana.Media!.Value, ana.Recuperacao!.Value, ana.NotaPeriodo!.Value, ana.RecuperacaoAplicada));
    }

    [Fact]
    public async Task LancarNotas_AlunoForaDaTurma_RetornaErro()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId));

        var result = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, 999, 5m)));

        Assert.Equal(NotaResultError.Validation, result.Error);
        Assert.Contains("não está na turma", result.Message);
    }

    [Fact]
    public async Task LancarNotas_AvaliacaoDeOutroPeriodo_RetornaErro()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, periodoId: Fixture.Periodo2Id));

        var result = await f.Service.LancarNotasAsync(Gestao, f.Lancamento((1, Fixture.AnaId, 5m)));

        Assert.Contains("não pertence a esta turma, disciplina e período", result.Message);
    }

    [Fact]
    public async Task LancarNotas_Valida_GravaInclusoesEExclusoes()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId));
        IReadOnlyCollection<NotaInformada>? gravadas = null;
        f.Avaliacoes.Setup(r => r.SalvarNotasAsync(It.IsAny<IReadOnlyCollection<NotaInformada>>(), UsuarioProfessor.UsuarioId))
            .Callback<IReadOnlyCollection<NotaInformada>, int>((n, _) => gravadas = n)
            .Returns(Task.CompletedTask);

        var result = await f.Service.LancarNotasAsync(UsuarioProfessor, f.Lancamento((1, Fixture.AnaId, 7.25m), (1, Fixture.BrunoId, null)));

        Assert.True(result.Success, result.Message);
        Assert.Equal([new NotaInformada(1, Fixture.AnaId, 7.25m), new NotaInformada(1, Fixture.BrunoId, null)], gravadas);
    }

    [Fact]
    public async Task GetPeriodo_CalculaNotaDoPeriodoComPesosERecuperacao()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, peso: 2m, notas: [(Fixture.AnaId, 8m), (Fixture.BrunoId, 3m)]));
        f.Lista.Add(f.NovaAvaliacao(2, Fixture.MatematicaId, peso: 1m, notas: [(Fixture.AnaId, 5m)]));
        f.Lista.Add(f.NovaAvaliacao(3, Fixture.MatematicaId, tipo: Avaliacao.TipoRecuperacao, notas: [(Fixture.BrunoId, 6.5m)]));

        var result = await f.Service.GetPeriodoAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Fixture.Periodo1Id);

        Assert.True(result.Success, result.Message);
        var ana = result.Value!.Alunos.Single(a => a.AlunoId == Fixture.AnaId);
        var bruno = result.Value.Alunos.Single(a => a.AlunoId == Fixture.BrunoId);
        Assert.Equal((7m, 0, false), (ana.NotaPeriodo!.Value, ana.Pendentes, ana.AbaixoDaMedia));
        Assert.Equal((3m, 6.5m, 6.5m, 1), (bruno.Media!.Value, bruno.Recuperacao!.Value, bruno.NotaPeriodo!.Value, bruno.Pendentes));
        Assert.True(result.Value.Avaliacoes.Last().Recuperacao);
    }

    [Fact]
    public async Task GetMedias_CalculaMediaFinalESituacao()
    {
        var f = new Fixture();
        f.Lista.Add(f.NovaAvaliacao(1, Fixture.MatematicaId, notas: [(Fixture.AnaId, 8m), (Fixture.BrunoId, 4m)]));
        f.Lista.Add(f.NovaAvaliacao(2, Fixture.MatematicaId, periodoId: Fixture.Periodo2Id, notas: [(Fixture.AnaId, 6m), (Fixture.BrunoId, 5m)]));

        var result = await f.Service.GetMediasAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId);

        Assert.True(result.Success, result.Message);
        var ana = result.Value!.Alunos.Single(a => a.AlunoId == Fixture.AnaId);
        var bruno = result.Value.Alunos.Single(a => a.AlunoId == Fixture.BrunoId);
        Assert.Equal((7m, true, CalculoNotas.SituacaoMediaAtingida), (ana.MediaFinal!.Value, ana.Completa, ana.Situacao));
        Assert.Equal((4.5m, CalculoNotas.SituacaoAbaixoDaMedia), (bruno.MediaFinal!.Value, bruno.Situacao));
    }

    [Fact]
    public async Task GetPeriodo_AlunoInativoSemNota_FicaForaDaLista()
    {
        var f = new Fixture();
        f.Alunos[Fixture.BrunoId].Status = Aluno.StatusInativo;

        var result = await f.Service.GetPeriodoAsync(Gestao, Fixture.TurmaId, Fixture.MatematicaId, Fixture.Periodo1Id);

        Assert.Equal([Fixture.AnaId], result.Value!.Alunos.Select(a => a.AlunoId));
    }

    /// <summary>
    /// Turma "6º Ano A" (ano com 2 bimestres; o 1º termina ontem) com Ana e Bruno; grade com Matemática e
    /// História. O usuário 2 é professor alocado só em Matemática; o usuário 1 é da gestão.
    /// </summary>
    private sealed class Fixture
    {
        public const int TurmaId = 10, MatematicaId = 20, HistoriaId = 21, AnaId = 100, BrunoId = 101, EtapaId = 5;
        public const int Periodo1Id = 31, Periodo2Id = 32;

        public Mock<IAvaliacaoRepository> Avaliacoes { get; } = new();
        public Mock<IRegraAvaliacaoRepository> Regras { get; } = new();
        public List<Avaliacao> Lista { get; } = new();
        public Dictionary<int, Aluno> Alunos { get; }

        /// <summary>Alunos que deixaram a turma: fim do vínculo e motivo da saída.</summary>
        public Dictionary<int, (DateOnly Fim, string? Motivo)> Saidas { get; } = new();
        public Turma Turma { get; }
        public NotaService Service { get; }

        public Fixture()
        {
            var anoLetivo = new AnoLetivo
            {
                Id = 1, AnoReferencia = Hoje.Year, DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(60),
                TipoPeriodo = AnoLetivo.TipoBimestral,
                Periodos =
                [
                    new PeriodoAvaliativo { Id = Periodo1Id, AnoLetivoId = 1, Nome = "1º Bimestre", Numero = 1, DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(-1) },
                    new PeriodoAvaliativo { Id = Periodo2Id, AnoLetivoId = 1, Nome = "2º Bimestre", Numero = 2, DataInicio = Hoje, DataTermino = Hoje.AddDays(60) },
                ],
            };
            Turma = new Turma
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
            turmas.Setup(r => r.GetByIdAsync(TurmaId)).ReturnsAsync(Turma);
            turmas.Setup(r => r.GetAllAsync()).ReturnsAsync([Turma]);

            var disciplinas = new Mock<IDisciplinaRepository>();
            disciplinas.Setup(r => r.GetByIdAsync(MatematicaId)).ReturnsAsync(matematica);
            disciplinas.Setup(r => r.GetByIdAsync(HistoriaId)).ReturnsAsync(historia);
            disciplinas.Setup(r => r.GetAllAsync()).ReturnsAsync([matematica, historia]);

            var anos = new Mock<IAnoLetivoRepository>();
            anos.Setup(r => r.GetAllAsync()).ReturnsAsync([anoLetivo]);
            anos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(anoLetivo);

            var professores = new Mock<IProfessorRepository>();
            professores.Setup(r => r.GetByUsuarioIdAsync(UsuarioProfessor.UsuarioId)).ReturnsAsync(new Professor { Id = 7 });

            var alocacoes = new Mock<IProfessorAlocacaoRepository>();
            alocacoes.Setup(r => r.GetByProfessorIdAsync(7))
                .ReturnsAsync([new ProfessorAlocacao { ProfessorId = 7, TurmaId = TurmaId, DisciplinaId = MatematicaId }]);

            Regras.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
            Avaliacoes.Setup(r => r.GetEnturmacoesAsync(TurmaId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .ReturnsAsync(() => Alunos.Values.Select(a => new AlunoTurma
                {
                    AlunoId = a.Id, Aluno = a, TurmaId = TurmaId,
                    DataFim = Saidas.TryGetValue(a.Id, out var saida) ? saida.Fim : null,
                    MotivoDesenturmacao = Saidas.TryGetValue(a.Id, out saida) ? saida.Motivo : null,
                }).ToList());
            Avaliacoes.Setup(r => r.ListAsync(TurmaId, It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync((int _, int disciplinaId, int? periodoId) => Lista
                    .Where(a => a.DisciplinaId == disciplinaId && (periodoId == null || a.PeriodoAvaliativoId == periodoId))
                    .ToList());

            Service = new NotaService(Avaliacoes.Object, Regras.Object, turmas.Object, disciplinas.Object, anos.Object,
                professores.Object, alocacoes.Object);
        }

        public void PrazoDoPeriodo1(DateOnly prazo)
            => Turma.AnoLetivo.Periodos.Single(p => p.Id == Periodo1Id).PrazoLancamentoNotas = prazo;

        public void UsarRegra(RegraAvaliacao regra)
            => Regras.Setup(r => r.GetByEtapaAsync(EtapaId)).ReturnsAsync(regra);

        public AvaliacaoRequest Requisicao(int disciplinaId) => new()
        {
            TurmaId = TurmaId, DisciplinaId = disciplinaId, PeriodoAvaliativoId = Periodo1Id,
            Nome = "Prova", Tipo = Avaliacao.TipoProva,
        };

        public Avaliacao NovaAvaliacao(
            int id, int disciplinaId, int periodoId = Periodo1Id, string tipo = Avaliacao.TipoProva,
            decimal peso = 1m, decimal? valorMaximo = null, string nome = "Avaliação", params (int AlunoId, decimal Valor)[] notas)
        {
            return new Avaliacao
            {
                Id = id, TurmaId = TurmaId, DisciplinaId = disciplinaId, PeriodoAvaliativoId = periodoId, Nome = nome,
                Tipo = tipo, Peso = peso, ValorMaximo = valorMaximo,
                Notas = notas.Select(n => new NotaAvaliacao { AvaliacaoId = id, AlunoId = n.AlunoId, Aluno = Alunos[n.AlunoId], Valor = n.Valor }).ToList(),
            };
        }

        public LancamentoNotasRequest Lancamento(params (int AvaliacaoId, int AlunoId, decimal? Valor)[] notas) => new()
        {
            TurmaId = TurmaId, DisciplinaId = MatematicaId, PeriodoAvaliativoId = Periodo1Id,
            Notas = notas.Select(n => new NotaLancamentoRequest { AvaliacaoId = n.AvaliacaoId, AlunoId = n.AlunoId, Valor = n.Valor }).ToList(),
        };

        private static Disciplina Disciplina(int id, string nome) => new()
        {
            Id = id, Nome = nome, Ativa = true,
            EtapasEnsino = [new DisciplinaEtapaEnsino { DisciplinaId = id, EtapaEnsinoId = EtapaId }],
        };
    }
}
