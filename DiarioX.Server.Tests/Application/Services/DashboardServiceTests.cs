using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Dashboard;
using DiarioX.Server.Application.DTOs.Dashboard;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class DashboardServiceTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 26);
    private static readonly IndicadoresLinha Zerados = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private readonly Mock<IDashboardConsultas> _consultas = new();
    private readonly Mock<IPermissaoService> _permissoes = new();
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        ComIndicadores(Zerados);
        _consultas.Setup(c => c.ListarMarcosDoCalendarioAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync([]);
        _consultas.Setup(c => c.ListarUltimasChamadasAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync([]);
        _service = new DashboardService(_consultas.Object, _permissoes.Object);
    }

    [Fact]
    public async Task SemPermissoes_NaoPreencheBlocos()
    {
        ComPermissoes();
        ComIndicadores(new IndicadoresLinha(10, 8, 3, 2, 1, 4, 5, 20, 10, 2, 1, 1));

        var painel = await _service.ObterAsync(new UsuarioAtual(1, false), Hoje);

        Assert.Null(painel.Alunos);
        Assert.Null(painel.Turmas);
        Assert.Null(painel.Frequencia);
        Assert.Null(painel.Professores);
        Assert.Null(painel.UltimasChamadas);
        Assert.Empty(painel.Alertas);
        _consultas.Verify(c => c.ListarUltimasChamadasAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task Indicadores_SaoConsultadosNumaUnicaChamadaComAsJanelasDoPainel()
    {
        ComPermissoes(Permissoes.Chamada.Visualizar);
        _consultas.Setup(c => c.ObterProfessorIdDoUsuarioAsync(7)).ReturnsAsync(42);

        await _service.ObterAsync(new UsuarioAtual(7, false), Hoje);

        _consultas.Verify(c => c.ObterIndicadoresAsync(new ParametrosIndicadores(
            Hoje, Hoje.AddDays(-30), Hoje.AddDays(-29), Hoje.AddDays(-7), 75m, 4, 42)), Times.Once);
    }

    [Fact]
    public async Task Alunos_CalculaVariacaoEmRelacaoA30DiasAtras()
    {
        ComPermissoes(Permissoes.Alunos.Visualizar);
        ComIndicadores(Zerados with { AlunosAtivos = 1240, AlunosAtivosNaComparacao = 1200 });

        var painel = await _service.ObterAsync(new UsuarioAtual(1, false), Hoje);

        Assert.Equal(1240, painel.Alunos!.Ativos);
        Assert.Equal(3.3m, painel.Alunos.VariacaoPercentual);
    }

    [Fact]
    public async Task Alunos_SemBaseDeComparacao_NaoInformaVariacao()
    {
        ComPermissoes(Permissoes.Alunos.Visualizar);
        ComIndicadores(Zerados with { AlunosAtivos = 10 });

        var painel = await _service.ObterAsync(new UsuarioAtual(1, false), Hoje);

        Assert.Null(painel.Alunos!.VariacaoPercentual);
    }

    [Fact]
    public async Task Alunos_AguardandoEnturmacao_GeraAviso()
    {
        ComPermissoes(Permissoes.Alunos.Visualizar);
        ComIndicadores(Zerados with { AlunosAguardandoEnturmacao = 3 });

        var painel = await _service.ObterAsync(new UsuarioAtual(1, false), Hoje);

        var alerta = Assert.Single(painel.Alertas);
        Assert.Equal("3 alunos aguardam enturmação.", alerta.Mensagem);
        Assert.Equal("enturmar-aluno", alerta.Pagina);
    }

    [Fact]
    public async Task Chamada_FrequenciaEAlertasFicamRestritosAoProfessorDoUsuario()
    {
        ComPermissoes(Permissoes.Chamada.Visualizar);
        _consultas.Setup(c => c.ObterProfessorIdDoUsuarioAsync(7)).ReturnsAsync(42);
        ComIndicadores(Zerados with
        {
            AulasRegistradas = 200, Presencas = 189, DisciplinasSemChamada = 3, TurmasSemChamada = 2, AlunosInfrequentes = 1,
        });

        var painel = await _service.ObterAsync(new UsuarioAtual(7, false), Hoje);

        Assert.Equal(94.5m, painel.Frequencia!.Percentual);
        Assert.Equal(75m, painel.Frequencia.Meta);
        Assert.Collection(painel.Alertas,
            a =>
            {
                Assert.Equal(TipoAlerta.Atencao, a.Tipo);
                Assert.Equal("Atenção: 2 turmas sem chamada há mais de 7 dias (3 disciplinas).", a.Mensagem);
                Assert.Equal("chamada", a.Pagina);
            },
            a => Assert.Equal("1 aluno está com frequência abaixo de 75% nos últimos 30 dias.", a.Mensagem));
        _consultas.Verify(c => c.ListarUltimasChamadasAsync(5, 42), Times.Once);
    }

    [Fact]
    public async Task Chamada_SemAulasNoPeriodo_FrequenciaNula()
    {
        ComPermissoes(Permissoes.Chamada.Visualizar);

        var painel = await _service.ObterAsync(new UsuarioAtual(1, true), Hoje);

        Assert.Null(painel.Frequencia!.Percentual);
        Assert.Empty(painel.Alertas);
        // Administrador global não é tratado como professor.
        _consultas.Verify(c => c.ObterProfessorIdDoUsuarioAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Calendario_AvisaFimDePeriodoProximoEMontaAgendaOrdenada()
    {
        ComPermissoes();
        _consultas.Setup(c => c.ListarMarcosDoCalendarioAsync(Hoje, Hoje.AddDays(90))).ReturnsAsync([
            new MarcoCalendarioLinha(2026, "3º Bimestre", new DateOnly(2026, 7, 27), Hoje.AddDays(5)),
            new MarcoCalendarioLinha(2026, "4º Bimestre", Hoje.AddDays(6), new DateOnly(2027, 1, 30)),
            new MarcoCalendarioLinha(2026, null, new DateOnly(2026, 2, 2), new DateOnly(2026, 12, 18)),
        ]);

        var painel = await _service.ObterAsync(new UsuarioAtual(1, false), Hoje);

        var alerta = Assert.Single(painel.Alertas);
        Assert.Equal(TipoAlerta.Info, alerta.Tipo);
        Assert.Equal("3º Bimestre de 2026 termina em 5 dias (01/10). Confira os lançamentos pendentes.", alerta.Mensagem);
        Assert.Equal(
            ["Término: 3º Bimestre", "Início: 4º Bimestre", "Encerramento do ano letivo 2026"],
            painel.Agenda.Select(e => e.Titulo));
    }

    private void ComPermissoes(params string[] permissoes)
        => _permissoes.Setup(p => p.GetPermissoesDoUsuarioAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(permissoes.ToHashSet());

    private void ComIndicadores(IndicadoresLinha indicadores)
        => _consultas.Setup(c => c.ObterIndicadoresAsync(It.IsAny<ParametrosIndicadores>())).ReturnsAsync(indicadores);
}
