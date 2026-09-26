using ClosedXML.Excel;
using DiarioX.Server.Application.Auth;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Relatorios;
using DiarioX.Server.Application.Relatorios.Definicoes;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using DiarioX.Server.Infrastructure.Relatorios;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class RelatorioServiceTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 26);
    private static readonly UsuarioAtual Usuario = new(7, IsGlobalAdmin: false);

    [Fact]
    public async Task Ocupacao_CalculaDisponiveisOcupacaoEIndicadores()
    {
        var consultas = new Mock<IRelatorioConsultas>();
        consultas.Setup(c => c.ListarOcupacaoDasTurmasAsync(null, null, null, Hoje)).ReturnsAsync(
        [
            new OcupacaoTurmaLinha(1, "1º Ano A", "Escola Norte", "1º Ano", Turma.TurnoManha, 20, 15),
            new OcupacaoTurmaLinha(2, "2º Ano A", "Escola Sul", "2º Ano", Turma.TurnoTarde, 10, 12),
        ]);

        var resultado = await new OcupacaoVagasRelatorio(consultas.Object).GerarAsync(new RelatorioFiltros(), Hoje);

        var conteudo = resultado.Value!;
        Assert.Equal(new object?[] { "Escola Norte", "1º Ano A", "Manhã", 20, 15, 5, 75.0m }, conteudo.Linhas[0]);
        // Turma com mais alunos que vagas: nenhuma vaga disponível e ocupação acima de 100%.
        Assert.Equal(new object?[] { 0, 120.0m }, conteudo.Linhas[1][5..]);
        Assert.Contains(new IndicadorRelatorio("Ocupação geral", "90,0%"), conteudo.Indicadores);
        Assert.Contains(new IndicadorRelatorio("Turmas lotadas", "1"), conteudo.Indicadores);
        Assert.Equal(["Escola Norte", "Escola Sul"], conteudo.Grafico!.Categorias);
    }

    [Fact]
    public async Task Ocupacao_TurnoInvalido_RetornaErroDeValidacao()
    {
        var resultado = await new OcupacaoVagasRelatorio(Mock.Of<IRelatorioConsultas>())
            .GerarAsync(new RelatorioFiltros { Turno = "madrugada" }, Hoje);

        Assert.Equal(RelatorioErro.Validation, resultado.Error);
    }

    [Fact]
    public async Task RelacaoAlunos_ExigeTurma()
    {
        var resultado = await new RelacaoAlunosTurmaRelatorio(Mock.Of<IRelatorioConsultas>())
            .GerarAsync(new RelatorioFiltros(), Hoje);

        Assert.Equal(RelatorioErro.Validation, resultado.Error);
        Assert.Equal("Selecione a turma.", resultado.Message);
    }

    [Fact]
    public async Task AlunosAguardando_OrdenaPelaMaiorEspera()
    {
        var consultas = new Mock<IRelatorioConsultas>();
        consultas.Setup(c => c.ListarAlunosAguardandoEnturmacaoAsync(null)).ReturnsAsync(
        [
            new AlunoAguardandoLinha("20260001", "Recente", "Escola", new DateTime(2019, 1, 1), new DateTime(2026, 9, 20)),
            new AlunoAguardandoLinha("20260002", "Antigo", "Escola", new DateTime(2019, 1, 1), new DateTime(2026, 8, 27)),
        ]);

        var resultado = await new AlunosAguardandoEnturmacaoRelatorio(consultas.Object).GerarAsync(new RelatorioFiltros(), Hoje);

        Assert.Equal(new object?[] { "Antigo", "Recente" }, resultado.Value!.Linhas.Select(l => l[1]));
        Assert.Equal(30, resultado.Value.Linhas[0][5]);
        Assert.Contains(new IndicadorRelatorio("Maior espera (dias)", "30"), resultado.Value.Indicadores);
    }

    [Fact]
    public async Task Listar_OcultaRelatoriosSemPermissaoDoModulo()
    {
        var service = BuildService(permissoes: [Permissoes.Relatorios.Visualizar, Permissoes.Turmas.Visualizar]);

        var catalogo = await service.ListarAsync(Usuario);

        Assert.Equal(["ocupacao-vagas"], catalogo.Select(r => r.Id));
    }

    [Fact]
    public async Task Gerar_SemPermissaoDoModulo_RetornaForbidden()
    {
        var service = BuildService(permissoes: [Permissoes.Relatorios.Visualizar]);

        var resultado = await service.GerarAsync(Usuario, "ocupacao-vagas", new RelatorioFiltros());

        Assert.Equal(RelatorioErro.Forbidden, resultado.Error);
    }

    [Fact]
    public async Task Gerar_RelatorioInexistente_RetornaNotFound()
    {
        var resultado = await BuildService().GerarAsync(Usuario, "nao-existe", new RelatorioFiltros());

        Assert.Equal(RelatorioErro.NotFound, resultado.Error);
    }

    [Fact]
    public async Task Gerar_PreencheInstituicaoETitulo()
    {
        var resultado = await BuildService().GerarAsync(Usuario, "ocupacao-vagas", new RelatorioFiltros());

        Assert.True(resultado.Success);
        Assert.Equal("Rede Municipal", resultado.Value!.Instituicao);
        Assert.Equal("Ocupação de vagas", resultado.Value.Titulo);
    }

    [Fact]
    public void Exportar_Excel_GravaNumerosEDatasComoValores()
    {
        var arquivo = BuildService().Exportar(Amostra(), "xlsx").Value!;

        using var workbook = new XLWorkbook(new MemoryStream(arquivo.Conteudo));
        var planilha = workbook.Worksheet(1);
        var cabecalho = planilha.CellsUsed().First(c => c.GetString() == "Vagas");
        var vagas = cabecalho.CellBelow();
        Assert.Equal(20, vagas.GetValue<int>());
        Assert.Equal(0.75, cabecalho.CellRight().CellRight().CellBelow().GetValue<double>(), 3);
        Assert.Equal(new DateTime(2026, 2, 1), cabecalho.CellRight().CellBelow().GetDateTime());
        Assert.StartsWith("ocupacao-vagas-20260926-", arquivo.NomeArquivo);
    }

    [Fact]
    public void Exportar_Pdf_GeraDocumento()
    {
        var arquivo = BuildService().Exportar(Amostra(), "PDF").Value!;

        Assert.Equal("application/pdf", arquivo.ContentType);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(arquivo.Conteudo, 0, 4));
    }

    [Fact]
    public void Exportar_FormatoDesconhecido_RetornaErroDeValidacao()
    {
        var resultado = BuildService().Exportar(Amostra(), "docx");

        Assert.Equal(RelatorioErro.Validation, resultado.Error);
    }

    private static RelatorioGerado Amostra() => new(
        "ocupacao-vagas", "Ocupação de vagas", "Rede Municipal", new DateTime(2026, 9, 26, 10, 30, 0), false,
        [new FiltroAplicado("Escola", "Escola Norte")],
        [new ColunaRelatorio("Turma"), new ColunaRelatorio("Vagas", TiposColuna.Inteiro),
         new ColunaRelatorio("Desde", TiposColuna.Data), new ColunaRelatorio("Ocupação", TiposColuna.Percentual)],
        [["1º Ano A", 20, new DateOnly(2026, 2, 1), 75.0m]],
        [new IndicadorRelatorio("Turmas", "1")],
        null);

    private static RelatorioService BuildService(HashSet<string>? permissoes = null)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var consultas = new Mock<IRelatorioConsultas>();
        consultas.Setup(c => c.ListarOcupacaoDasTurmasAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<DateOnly>()))
            .ReturnsAsync([]);

        var permissaoService = new Mock<IPermissaoService>();
        permissaoService.Setup(p => p.GetPermissoesDoUsuarioAsync(Usuario.UsuarioId, false))
            .ReturnsAsync(permissoes as IReadOnlySet<string> ?? Permissoes.Todas);

        var tenantRepository = new Mock<ITenantRepository>();
        tenantRepository.Setup(t => t.GetByIdAsync(3)).ReturnsAsync(new Tenant { Id = 3, Nome = "Rede Municipal" });
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(t => t.TenantId).Returns(3);

        IRelatorio[] relatorios =
        [
            new OcupacaoVagasRelatorio(consultas.Object),
            new RelacaoAlunosTurmaRelatorio(consultas.Object),
            new AlunosAguardandoEnturmacaoRelatorio(consultas.Object),
        ];

        return new RelatorioService(
            relatorios,
            [new ExcelExportador(), new PdfExportador()],
            consultas.Object,
            permissaoService.Object,
            tenantRepository.Object,
            tenantContext.Object);
    }
}
