using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.DTOs.Conteudos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;
using Xunit;

namespace DiarioX.Server.Tests.Application.Services;

public class ConteudoMinistradoServiceTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);
    private static readonly UsuarioAtual Gestao = new(UsuarioId: 1, IsGlobalAdmin: false);
    private static readonly UsuarioAtual UsuarioProfessor = new(UsuarioId: 2, IsGlobalAdmin: false);

    // ---------- Acesso ----------

    [Fact]
    public async Task GetTurmas_Professor_VeApenasSuasAlocacoes()
    {
        var f = new Fixture();

        var turma = Assert.Single(await f.Service.GetTurmasAsync(UsuarioProfessor));

        Assert.Equal(["Matemática"], turma.Disciplinas.Select(d => d.Nome));
    }

    [Fact]
    public async Task GetTurmas_FrequenciaDiaria_ProfessorAlocadoNaTurmaVeToda_AGrade()
    {
        var f = new Fixture(EtapaEnsino.FrequenciaDiaria);

        var turma = Assert.Single(await f.Service.GetTurmasAsync(UsuarioProfessor));

        Assert.Equal(["História", "Matemática"], turma.Disciplinas.Select(d => d.Nome));
        Assert.Equal(EtapaEnsino.FrequenciaDiaria, turma.TipoFrequencia);
    }

    [Fact]
    public async Task Create_ProfessorEmDisciplinaNaoAlocada_RetornaForbidden()
    {
        var f = new Fixture();

        var result = await f.Service.CreateAsync(UsuarioProfessor, f.Request(Fixture.HistoriaId));

        Assert.Equal(ConteudoResultError.Forbidden, result.Error);
        f.Conteudos.Verify(r => r.AddAsync(It.IsAny<ConteudoMinistrado>()), Times.Never);
    }

    // ---------- Registro ----------

    [Fact]
    public async Task Create_Valido_GravaConteudoComHabilidades()
    {
        var f = new Fixture();
        ConteudoMinistrado? gravado = null;
        f.Conteudos.Setup(r => r.AddAsync(It.IsAny<ConteudoMinistrado>()))
            .Callback<ConteudoMinistrado>(c => gravado = c).ReturnsAsync((ConteudoMinistrado c) => c);
        var request = f.Request(Fixture.MatematicaId, "  Frações equivalentes  ");
        request.HabilidadesIds = [Fixture.HabilidadeId, Fixture.HabilidadeId];

        var result = await f.Service.CreateAsync(UsuarioProfessor, request);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(gravado);
        Assert.Equal("Frações equivalentes", gravado!.Descricao);
        Assert.Equal(UsuarioProfessor.UsuarioId, gravado.RegistradoPorUsuarioId);
        Assert.Equal([Fixture.HabilidadeId], gravado.Habilidades.Select(h => h.HabilidadeBnccId));
    }

    [Fact]
    public async Task Create_SemDescricao_RetornaErro()
    {
        var f = new Fixture();

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId, "   "));

        Assert.Equal(ConteudoResultError.Validation, result.Error);
        Assert.Equal("Informe o conteúdo ministrado.", result.Message);
    }

    [Fact]
    public async Task Create_JaExisteNaData_RetornaConflict()
    {
        var f = new Fixture();
        f.Conteudos.Setup(r => r.GetAsync(Fixture.TurmaId, Fixture.MatematicaId, Hoje))
            .ReturnsAsync(f.Conteudo(Hoje, Fixture.MatematicaId, "Já registrado"));

        var result = await f.Service.CreateAsync(Gestao, f.Request(Fixture.MatematicaId));

        Assert.Equal(ConteudoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task Create_DataFutura_RetornaErro()
    {
        var f = new Fixture();
        var request = f.Request(Fixture.MatematicaId);
        request.Data = Hoje.AddDays(1);

        var result = await f.Service.CreateAsync(Gestao, request);

        Assert.Equal("A data do conteúdo não pode ser futura.", result.Message);
    }

    [Fact]
    public async Task Create_HabilidadeInexistente_RetornaErro()
    {
        var f = new Fixture();
        var request = f.Request(Fixture.MatematicaId);
        request.HabilidadesIds = [Fixture.HabilidadeId, 999];

        var result = await f.Service.CreateAsync(Gestao, request);

        Assert.Equal("Habilidade da BNCC não encontrada.", result.Message);
        f.Conteudos.Verify(r => r.AddAsync(It.IsAny<ConteudoMinistrado>()), Times.Never);
    }

    [Fact]
    public async Task Create_HabilidadeInativa_RetornaErro()
    {
        var f = new Fixture();
        var request = f.Request(Fixture.MatematicaId);
        request.HabilidadesIds = [Fixture.HabilidadeInativaId];

        var result = await f.Service.CreateAsync(Gestao, request);

        Assert.False(result.Success);
        Assert.Contains("EF06MA02", result.Message);
    }

    [Fact]
    public async Task Update_MantemHabilidadeInativadaDepoisDeVinculada()
    {
        var f = new Fixture();
        var existente = f.Conteudo(Hoje, Fixture.MatematicaId, "Antigo", Fixture.HabilidadeInativaId);
        f.Conteudos.Setup(r => r.GetByIdAsync(existente.Id)).ReturnsAsync(existente);
        var request = f.Request(Fixture.MatematicaId, "Novo texto");
        request.HabilidadesIds = [Fixture.HabilidadeInativaId, Fixture.HabilidadeId];

        var result = await f.Service.UpdateAsync(Gestao, existente.Id, request);

        Assert.True(result.Success, result.Message);
        f.Conteudos.Verify(r => r.UpdateAsync(
            It.Is<ConteudoMinistrado>(c => c.Descricao == "Novo texto" && c.AtualizadoPorUsuarioId == Gestao.UsuarioId),
            It.Is<IEnumerable<int>>(ids => ids.OrderBy(i => i).SequenceEqual(new[] { Fixture.HabilidadeId, Fixture.HabilidadeInativaId }))),
            Times.Once);
    }

    // ---------- EX01 ----------

    [Fact]
    public async Task DiaSemAulaNoCalendario_BloqueiaRegistroEMostraAlerta()
    {
        var f = new Fixture();
        f.PublicarEventoSemAula(Hoje, "Conselho de Classe");
        const string mensagem =
            "Não é possível realizar lançamentos nesta data. Evento cadastrado no Calendário Escolar: Conselho de Classe - Dia Sem Aula.";

        var aula = await f.Service.GetAsync(UsuarioProfessor, Fixture.TurmaId, Fixture.MatematicaId, Hoje);
        var criacao = await f.Service.CreateAsync(UsuarioProfessor, f.Request(Fixture.MatematicaId));

        Assert.Equal(mensagem, aula.Value!.Bloqueio);
        Assert.False(criacao.Success);
        Assert.Equal(mensagem, criacao.Message);
        f.Conteudos.Verify(r => r.AddAsync(It.IsAny<ConteudoMinistrado>()), Times.Never);
    }

    [Fact]
    public async Task DiaSemAulaNoCalendario_BloqueiaEdicao()
    {
        var f = new Fixture();
        f.PublicarEventoSemAula(Hoje, "Recesso");
        var existente = f.Conteudo(Hoje, Fixture.MatematicaId, "Texto");
        f.Conteudos.Setup(r => r.GetByIdAsync(existente.Id)).ReturnsAsync(existente);

        var result = await f.Service.UpdateAsync(Gestao, existente.Id, f.Request(Fixture.MatematicaId, "Outro texto"));

        Assert.False(result.Success);
        Assert.Contains("Recesso - Dia Sem Aula", result.Message);
        f.Conteudos.Verify(r => r.UpdateAsync(It.IsAny<ConteudoMinistrado>(), It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    // ---------- RN01: diário ----------

    [Fact]
    public async Task Diario_SinalizaFrequenciaSemConteudoEViceVersa()
    {
        var f = new Fixture();
        var completo = Hoje.AddDays(-1);
        var soFrequencia = Hoje.AddDays(-2);
        var soConteudo = Hoje.AddDays(-3);
        f.Chamadas.Setup(r => r.ListAsync(Fixture.TurmaId, Fixture.MatematicaId, It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync([f.Chamada(completo, Fixture.MatematicaId), f.Chamada(soFrequencia, Fixture.MatematicaId)]);
        f.Conteudos.Setup(r => r.ListAsync(Fixture.TurmaId, Fixture.MatematicaId, It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync([f.Conteudo(completo, Fixture.MatematicaId, "Frações"), f.Conteudo(soConteudo, Fixture.MatematicaId, "Decimais")]);

        var result = await f.Service.GetDiarioAsync(UsuarioProfessor, Fixture.TurmaId, Fixture.MatematicaId, null, null);

        Assert.True(result.Success, result.Message);
        var diario = result.Value!;
        Assert.Equal([completo, soFrequencia, soConteudo], diario.Dias.Select(d => d.Data));
        Assert.Null(diario.Dias[0].Pendencia);
        Assert.Equal(DiarioPendencia.SemConteudo, diario.Dias[1].Pendencia);
        Assert.Equal("Frequência lançada sem registro de conteúdo ministrado.", diario.Dias[1].Alerta);
        Assert.Equal(DiarioPendencia.SemFrequencia, diario.Dias[2].Pendencia);
        Assert.Equal("Conteúdo ministrado registrado sem frequência lançada.", diario.Dias[2].Alerta);
        Assert.Equal(2, diario.TotalPendencias);
        Assert.Equal(2, diario.Dias[0].Frequencia!.Presentes);
    }

    [Fact]
    public async Task Diario_FrequenciaDiaria_ConfereAChamadaDoDiaComConteudoDeQualquerDisciplina()
    {
        var f = new Fixture(EtapaEnsino.FrequenciaDiaria);
        var comConteudo = Hoje.AddDays(-1);
        var semConteudo = Hoje.AddDays(-2);
        f.Chamadas.Setup(r => r.ListAsync(Fixture.TurmaId, null, It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync([f.Chamada(comConteudo, null), f.Chamada(semConteudo, null)]);
        f.Conteudos.Setup(r => r.ListAsync(Fixture.TurmaId, null, It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync([f.Conteudo(comConteudo, Fixture.MatematicaId, "Frações"), f.Conteudo(comConteudo, Fixture.HistoriaId, "Brasil Colônia")]);

        // Sem informar disciplina: a frequência diária não tem disciplina.
        var result = await f.Service.GetDiarioAsync(UsuarioProfessor, Fixture.TurmaId, null, null, null);

        Assert.True(result.Success, result.Message);
        var diario = result.Value!;
        Assert.Equal(2, diario.Dias.Count);
        Assert.Equal(2, diario.Dias.Single(d => d.Data == comConteudo).Conteudos.Count);
        Assert.Null(diario.Dias.Single(d => d.Data == comConteudo).Pendencia);
        Assert.Equal(DiarioPendencia.SemConteudo, diario.Dias.Single(d => d.Data == semConteudo).Pendencia);
        Assert.Equal(1, diario.TotalPendencias);
    }

    [Fact]
    public async Task Diario_PorAulaSemDisciplina_RetornaErro()
    {
        var f = new Fixture();

        var result = await f.Service.GetDiarioAsync(Gestao, Fixture.TurmaId, null, null, null);

        Assert.Equal(ConteudoResultError.Validation, result.Error);
    }

    // ---------- RN02: BNCC ----------

    [Fact]
    public async Task Sugestoes_UsaEtapaDaTurmaEDisciplinaSelecionada()
    {
        var f = new Fixture();

        var result = await f.Service.GetSugestoesAsync(UsuarioProfessor, Fixture.TurmaId, Fixture.MatematicaId, "fra");

        Assert.True(result.Success, result.Message);
        var sugestao = Assert.Single(result.Value!);
        Assert.Equal("EF06MA01", sugestao.Codigo);
        f.Habilidades.Verify(r => r.GetSugestoesAsync(Fixture.EtapaId, Fixture.MatematicaId, "fra"), Times.Once);
    }

    [Fact]
    public async Task Sugestoes_ProfessorEmDisciplinaNaoAlocada_RetornaForbidden()
    {
        var f = new Fixture();

        var result = await f.Service.GetSugestoesAsync(UsuarioProfessor, Fixture.TurmaId, Fixture.HistoriaId, null);

        Assert.Equal(ConteudoResultError.Forbidden, result.Error);
        f.Habilidades.Verify(r => r.GetSugestoesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    private sealed class Fixture
    {
        public const int TurmaId = 10, MatematicaId = 20, HistoriaId = 21, EtapaId = 5, HabilidadeId = 50, HabilidadeInativaId = 51;

        public Mock<IConteudoMinistradoRepository> Conteudos { get; } = new();
        public Mock<IHabilidadeBnccRepository> Habilidades { get; } = new();
        public Mock<IChamadaRepository> Chamadas { get; } = new();
        public Mock<ICalendarioLetivoRepository> Calendarios { get; } = new();
        public ConteudoMinistradoService Service { get; }

        private readonly Disciplina _matematica;
        private readonly Disciplina _historia;
        private readonly HabilidadeBncc _habilidade;
        private readonly HabilidadeBncc _habilidadeInativa;
        private int _proximoId = 1000;

        public Fixture(string tipoFrequencia = EtapaEnsino.FrequenciaPorAula)
        {
            var anoLetivo = new AnoLetivo
            {
                Id = 1, AnoReferencia = Hoje.Year,
                DataInicio = Hoje.AddDays(-60), DataTermino = Hoje.AddDays(60),
            };
            var turma = new Turma
            {
                Id = TurmaId, NomeCompleto = "6º Ano A", EtapaEnsinoId = EtapaId, Status = Turma.StatusAtivo,
                EtapaEnsino = new EtapaEnsino { Id = EtapaId, TipoFrequencia = tipoFrequencia },
                AnoLetivoId = 1, AnoLetivo = anoLetivo, Escola = new Escola { Nome = "Escola A" },
            };
            _matematica = Disciplina(MatematicaId, "Matemática");
            _historia = Disciplina(HistoriaId, "História");
            _habilidade = new HabilidadeBncc { Id = HabilidadeId, Codigo = "EF06MA01", Descricao = "Comparar números naturais e racionais.", DisciplinaId = MatematicaId, Ativa = true };
            _habilidadeInativa = new HabilidadeBncc { Id = HabilidadeInativaId, Codigo = "EF06MA02", Descricao = "Reconhecer sistemas de numeração.", DisciplinaId = MatematicaId, Ativa = false };

            var turmas = new Mock<ITurmaRepository>();
            turmas.Setup(r => r.GetByIdAsync(TurmaId)).ReturnsAsync(turma);
            turmas.Setup(r => r.GetAllAsync()).ReturnsAsync([turma]);

            var disciplinas = new Mock<IDisciplinaRepository>();
            disciplinas.Setup(r => r.GetByIdAsync(MatematicaId)).ReturnsAsync(_matematica);
            disciplinas.Setup(r => r.GetByIdAsync(HistoriaId)).ReturnsAsync(_historia);
            disciplinas.Setup(r => r.GetAllAsync()).ReturnsAsync([_matematica, _historia]);

            var anos = new Mock<IAnoLetivoRepository>();
            anos.Setup(r => r.GetAllAsync()).ReturnsAsync([anoLetivo]);
            anos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(anoLetivo);

            var professores = new Mock<IProfessorRepository>();
            professores.Setup(r => r.GetByUsuarioIdAsync(UsuarioProfessor.UsuarioId)).ReturnsAsync(new Professor { Id = 7 });

            var alocacoes = new Mock<IProfessorAlocacaoRepository>();
            alocacoes.Setup(r => r.GetByProfessorIdAsync(7))
                .ReturnsAsync([new ProfessorAlocacao { ProfessorId = 7, TurmaId = TurmaId, DisciplinaId = MatematicaId }]);

            Conteudos.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>())).ReturnsAsync([]);
            Habilidades.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> ids) => new[] { _habilidade, _habilidadeInativa }.Where(h => ids.Contains(h.Id)).ToList());
            Habilidades.Setup(r => r.GetSugestoesAsync(EtapaId, MatematicaId, It.IsAny<string?>())).ReturnsAsync([_habilidade]);

            Chamadas.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>())).ReturnsAsync([]);
            Chamadas.Setup(r => r.GetEmailsUsuariosAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Dictionary<int, string>());

            Calendarios.Setup(r => r.GetPublicadosAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync([]);

            Service = new ConteudoMinistradoService(Conteudos.Object, Habilidades.Object, Chamadas.Object, turmas.Object,
                disciplinas.Object, anos.Object, professores.Object, alocacoes.Object, Calendarios.Object);
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

        public ConteudoMinistradoRequest Request(int disciplinaId, string descricao = "Frações")
            => new() { TurmaId = TurmaId, DisciplinaId = disciplinaId, Data = Hoje, Descricao = descricao };

        public ConteudoMinistrado Conteudo(DateOnly data, int disciplinaId, string descricao, params int[] habilidadeIds)
        {
            var disciplina = disciplinaId == MatematicaId ? _matematica : _historia;
            var habilidades = new[] { _habilidade, _habilidadeInativa };
            return new ConteudoMinistrado
            {
                Id = _proximoId++, TurmaId = TurmaId, DisciplinaId = disciplinaId, Disciplina = disciplina, Data = data, Descricao = descricao,
                Habilidades = habilidadeIds
                    .Select(id => new ConteudoMinistradoHabilidade { HabilidadeBnccId = id, HabilidadeBncc = habilidades.Single(h => h.Id == id) })
                    .ToList(),
            };
        }

        public Chamada Chamada(DateOnly data, int? disciplinaId) => new()
        {
            Id = _proximoId++, TurmaId = TurmaId, DisciplinaId = disciplinaId, Data = data, QuantidadeAulas = 1,
            Registros =
            [
                new ChamadaAluno { Situacao = ChamadaAluno.SituacaoPresente },
                new ChamadaAluno { Situacao = ChamadaAluno.SituacaoPresente },
            ],
        };

        private static Disciplina Disciplina(int id, string nome) => new()
        {
            Id = id, Nome = nome, Ativa = true,
            EtapasEnsino = [new DisciplinaEtapaEnsino { DisciplinaId = id, EtapaEnsinoId = EtapaId }],
        };
    }
}
