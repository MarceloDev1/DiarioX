using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Calendario;
using DiarioX.Server.Application.DTOs.Calendario;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;
using Xunit;

namespace DiarioX.Server.Tests.Application.Services;

public class CalendarioLetivoServiceTests
{
    // 2026: 02/02 (segunda) a 18/12 (sexta).
    private static readonly DateOnly Inicio = new(2026, 2, 2);
    private static readonly DateOnly Termino = new(2026, 12, 18);
    private static readonly DateOnly Segunda = new(2026, 7, 13);
    private static readonly DateOnly Sabado = new(2026, 7, 18);
    private static readonly UsuarioAtual Usuario = new(UsuarioId: 1, IsGlobalAdmin: false);

    [Fact]
    public async Task Get_SemEventos_ContaDiasUteisDoAnoLetivo()
    {
        var f = new Fixture();

        var result = await f.Service.GetAsync(Fixture.AnoId, escolaId: null);

        Assert.True(result.Success, result.Message);
        Assert.Equal(DiasUteis(Inicio, Termino), result.Calendario!.DiasLetivos);
        Assert.Equal(200, result.Calendario.MetaDiasLetivos);
        Assert.False(result.Calendario.Publicado);
        Assert.Equal(4, result.Calendario.Periodos.Count);
    }

    [Fact]
    public async Task Get_FeriadoTiraESabadoLetivoSomaDiaLetivo()
    {
        var f = new Fixture();
        f.Rede.Eventos =
        [
            Evento(f.Rede, Segunda, EventoCalendario.TipoFeriado, "Feriado municipal", comAula: false),
            Evento(f.Rede, Sabado, EventoCalendario.TipoSabadoLetivo, "Feira de Ciências", comAula: true),
            Evento(f.Rede, Segunda.AddDays(1), EventoCalendario.TipoConselhoClasse, "I Conselho de Classe", comAula: true),
        ];

        var result = await f.Service.GetAsync(Fixture.AnoId, escolaId: null);

        Assert.Equal(DiasUteis(Inicio, Termino), result.Calendario!.DiasLetivos);
        Assert.Equal(3, result.Calendario.Eventos.Count);
        Assert.All(result.Calendario.Eventos, e => Assert.False(e.Herdado));
    }

    [Fact]
    public async Task Get_Escola_HerdaEventosDaRedePublicadaESubstituiNoMesmoDia()
    {
        var f = new Fixture();
        f.Rede.PublicadoEm = DateTime.UtcNow;
        f.Rede.Eventos =
        [
            Evento(f.Rede, Segunda, EventoCalendario.TipoFeriado, "Feriado da rede", comAula: false),
            Evento(f.Rede, Segunda.AddDays(1), EventoCalendario.TipoPontoFacultativo, "Ponto facultativo", comAula: false),
        ];
        f.Escola.Eventos = [Evento(f.Escola, Segunda.AddDays(1), EventoCalendario.TipoSabadoLetivo, "Reposição", comAula: true)];

        var result = await f.Service.GetAsync(Fixture.AnoId, Fixture.EscolaId);

        var eventos = result.Calendario!.Eventos;
        Assert.Equal(2, eventos.Count);
        Assert.True(eventos.Single(e => e.Data == Segunda).Herdado);
        var reposicao = eventos.Single(e => e.Data == Segunda.AddDays(1));
        Assert.False(reposicao.Herdado);
        Assert.Equal("Reposição", reposicao.Descricao);
        Assert.Equal(DiasUteis(Inicio, Termino) - 1, result.Calendario.DiasLetivos);
        Assert.True(result.Calendario.RedePublicada);
    }

    [Fact]
    public async Task Get_Escola_IgnoraEventosDaRedeNaoPublicada()
    {
        var f = new Fixture();
        f.Rede.Eventos = [Evento(f.Rede, Segunda, EventoCalendario.TipoFeriado, "Rascunho", comAula: false)];

        var result = await f.Service.GetAsync(Fixture.AnoId, Fixture.EscolaId);

        Assert.Empty(result.Calendario!.Eventos);
        Assert.False(result.Calendario.RedePublicada);
    }

    [Fact]
    public async Task SalvarEvento_Intervalo_AplicaEmTodosOsDias()
    {
        var f = new Fixture();
        List<EventoCalendario>? gravados = null;
        f.Calendarios.Setup(r => r.SubstituirEventosAsync(Fixture.RedeId, Segunda, Segunda.AddDays(10), It.IsAny<IEnumerable<EventoCalendario>>()))
            .Callback<int, DateOnly, DateOnly, IEnumerable<EventoCalendario>>((_, _, _, e) => gravados = e.ToList());

        var result = await f.Service.SalvarEventoAsync(new EventoCalendarioRequest
        {
            AnoLetivoId = Fixture.AnoId, DataInicio = Segunda, DataFim = Segunda.AddDays(10),
            Tipo = "recesso", Descricao = "  Recesso escolar de julho ", ComAula = false,
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal("Evento aplicado a 11 dias com sucesso!", result.Message);
        Assert.NotNull(gravados);
        Assert.Equal(11, gravados!.Count);
        Assert.All(gravados, e =>
        {
            Assert.Equal(EventoCalendario.TipoRecesso, e.Tipo);
            Assert.Equal("Recesso escolar de julho", e.Descricao);
            Assert.False(e.ComAula);
        });
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    public async Task SalvarEvento_ForaDoAnoLetivo_RetornaEx02(int diasAntesDoInicio, int diasDepoisDoTermino)
    {
        var f = new Fixture();

        var result = await f.Service.SalvarEventoAsync(new EventoCalendarioRequest
        {
            AnoLetivoId = Fixture.AnoId,
            DataInicio = diasAntesDoInicio < 0 ? Inicio.AddDays(diasAntesDoInicio) : Termino,
            DataFim = Termino.AddDays(diasDepoisDoTermino),
            Tipo = EventoCalendario.TipoFeriado, Descricao = "Feriado", ComAula = false,
        });

        Assert.False(result.Success);
        Assert.Equal("A data selecionada está fora do período do Ano Letivo configurado.", result.Message);
        f.Calendarios.Verify(r => r.SubstituirEventosAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
            It.IsAny<IEnumerable<EventoCalendario>>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "Feriado", false, "Selecione o tipo do evento.")]
    [InlineData("FERIADO", " ", false, "Informe a descrição (nome) do evento.")]
    [InlineData("FERIADO", "Feriado", null, "Informe se o dia é considerado letivo (com aula) ou sem aula.")]
    public async Task SalvarEvento_CamposObrigatorios(string? tipo, string descricao, bool? comAula, string mensagem)
    {
        var f = new Fixture();

        var result = await f.Service.SalvarEventoAsync(new EventoCalendarioRequest
        {
            AnoLetivoId = Fixture.AnoId, DataInicio = Segunda, Tipo = tipo, Descricao = descricao, ComAula = comAula,
        });

        Assert.False(result.Success);
        Assert.Equal(mensagem, result.Message);
    }

    [Fact]
    public async Task SalvarEvento_UsuarioComEscopoDeEscola_NaoAlteraCalendarioDaRede()
    {
        var f = new Fixture(escolasDoUsuario: [Fixture.EscolaId]);
        var request = new EventoCalendarioRequest
        {
            AnoLetivoId = Fixture.AnoId, DataInicio = Segunda, Tipo = EventoCalendario.TipoFeriado, Descricao = "Feriado", ComAula = false,
        };

        var naRede = await f.Service.SalvarEventoAsync(request);
        request.EscolaId = Fixture.EscolaId;
        var naEscola = await f.Service.SalvarEventoAsync(request);

        Assert.Equal(CalendarioResultError.Forbidden, naRede.Error);
        Assert.True(naEscola.Success, naEscola.Message);
    }

    [Fact]
    public async Task RemoverEventos_SemEventosNoIntervalo_RetornaErro()
    {
        var f = new Fixture();

        var result = await f.Service.RemoverEventosAsync(Fixture.AnoId, null, Segunda, Segunda.AddDays(3));

        Assert.False(result.Success);
        Assert.Equal("Não há eventos deste calendário nos dias selecionados.", result.Message);
    }

    [Fact]
    public async Task Publicar_RetornaMensagemDoAno()
    {
        var f = new Fixture();

        var result = await f.Service.PublicarAsync(Usuario, new CalendarioPublicacaoRequest { AnoLetivoId = Fixture.AnoId });

        Assert.True(result.Success);
        Assert.Equal("Calendário Letivo do Ano 2026 configurado e publicado com sucesso!", result.Message);
        f.Calendarios.Verify(r => r.PublicarAsync(Fixture.RedeId, Usuario.UsuarioId), Times.Once);
    }

    // ---------- Trava do diário (RN01/EX01) ----------

    [Fact]
    public void MotivoBloqueio_SemCalendarioPublicado_NaoBloqueia()
    {
        Assert.Null(CalendarioEfetivo.MotivoBloqueio(Sabado, []));
    }

    [Fact]
    public void MotivoBloqueio_DiaSemAula_RetornaMensagemDoEvento()
    {
        var rede = new CalendarioLetivo { Id = 1, PublicadoEm = DateTime.UtcNow };
        rede.Eventos = [Evento(rede, Segunda, EventoCalendario.TipoConselhoClasse, "Conselho de Classe", comAula: false)];

        Assert.Equal(
            "Não é possível realizar lançamentos nesta data. Evento cadastrado no Calendário Escolar: Conselho de Classe - Dia Sem Aula.",
            CalendarioEfetivo.MotivoBloqueio(Segunda, [rede]));
        Assert.Null(CalendarioEfetivo.MotivoBloqueio(Segunda.AddDays(1), [rede]));
    }

    [Fact]
    public void MotivoBloqueio_FimDeSemanaSoLiberaComSabadoLetivo()
    {
        var rede = new CalendarioLetivo { Id = 1, PublicadoEm = DateTime.UtcNow };
        var escola = new CalendarioLetivo { Id = 2, EscolaId = 5, PublicadoEm = DateTime.UtcNow };
        escola.Eventos = [Evento(escola, Sabado, EventoCalendario.TipoSabadoLetivo, "Feira de Ciências", comAula: true)];

        Assert.Contains("sábado sem Dia Letivo Especial", CalendarioEfetivo.MotivoBloqueio(Sabado, [rede]));
        Assert.Null(CalendarioEfetivo.MotivoBloqueio(Sabado, [rede, escola]));
        Assert.Contains("domingo", CalendarioEfetivo.MotivoBloqueio(Sabado.AddDays(1), [rede, escola]));
    }

    [Fact]
    public void MotivoBloqueio_EventoDaEscolaSubstituiODaRede()
    {
        var rede = new CalendarioLetivo { Id = 1, PublicadoEm = DateTime.UtcNow };
        rede.Eventos = [Evento(rede, Segunda, EventoCalendario.TipoPontoFacultativo, "Ponto facultativo", comAula: false)];
        var escola = new CalendarioLetivo { Id = 2, EscolaId = 5, PublicadoEm = DateTime.UtcNow };
        escola.Eventos = [Evento(escola, Segunda, EventoCalendario.TipoSabadoLetivo, "Reposição", comAula: true)];

        Assert.Null(CalendarioEfetivo.MotivoBloqueio(Segunda, [escola, rede]));
    }

    private static int DiasUteis(DateOnly de, DateOnly ate)
    {
        var total = 0;
        for (var dia = de; dia <= ate; dia = dia.AddDays(1))
            if (dia.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) total++;
        return total;
    }

    private static EventoCalendario Evento(CalendarioLetivo calendario, DateOnly data, string tipo, string descricao, bool comAula) => new()
    {
        CalendarioLetivoId = calendario.Id, Data = data, Tipo = tipo, Descricao = descricao, ComAula = comAula,
    };

    /// <summary>Ano letivo 2026 bimestral, calendário da rede (Id 1) e da escola (Id 2), ambos sem eventos.</summary>
    private sealed class Fixture
    {
        public const int AnoId = 1, EscolaId = 5, RedeId = 1, CalendarioEscolaId = 2;

        public Mock<ICalendarioLetivoRepository> Calendarios { get; } = new();
        public CalendarioLetivo Rede { get; } = new() { Id = RedeId, AnoLetivoId = AnoId };
        public CalendarioLetivo Escola { get; } = new() { Id = CalendarioEscolaId, AnoLetivoId = AnoId, EscolaId = EscolaId };
        public CalendarioLetivoService Service { get; }

        public Fixture(IReadOnlyList<int>? escolasDoUsuario = null)
        {
            var ano = new AnoLetivo
            {
                Id = AnoId, AnoReferencia = 2026, DataInicio = Inicio, DataTermino = Termino, TipoPeriodo = AnoLetivo.TipoBimestral,
                Periodos = Enumerable.Range(1, 4).Select(n => new PeriodoAvaliativo
                {
                    Id = n, Numero = n, Nome = $"{n}º Bimestre",
                    DataInicio = Inicio.AddDays((n - 1) * 70), DataTermino = Inicio.AddDays(n * 70 - 1),
                }).ToList(),
            };

            var anos = new Mock<IAnoLetivoRepository>();
            anos.Setup(r => r.GetByIdAsync(AnoId)).ReturnsAsync(ano);

            var escolas = new Mock<IEscolaRepository>();
            escolas.Setup(r => r.GetByIdAsync(EscolaId)).ReturnsAsync(new Escola { Id = EscolaId, Nome = "Escola Municipal A" });

            Calendarios.Setup(r => r.GetAsync(AnoId, null)).ReturnsAsync(() => Rede);
            Calendarios.Setup(r => r.GetAsync(AnoId, EscolaId)).ReturnsAsync(() => Escola);
            Calendarios.Setup(r => r.GetOrCreateAsync(AnoId, null)).ReturnsAsync(() => Rede);
            Calendarios.Setup(r => r.GetOrCreateAsync(AnoId, EscolaId)).ReturnsAsync(() => Escola);

            var tenant = new Mock<ITenantContext>();
            tenant.SetupGet(t => t.EscolaIds).Returns(escolasDoUsuario);

            Service = new CalendarioLetivoService(Calendarios.Object, anos.Object, escolas.Object, tenant.Object);
        }
    }
}
