using DiarioX.Server.Application.DTOs.Notas;
using DiarioX.Server.Application.Interfaces;
using DiarioX.Server.Application.Relatorios;
using DiarioX.Server.Application.Relatorios.Definicoes;
using Moq;
using Xunit;

namespace DiarioX.Server.Tests.Application.Relatorios;

public class MapaNotasRelatorioTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 26);

    [Fact]
    public async Task Gerar_UmaColunaPorDisciplina_EContaAsAbaixoDaMedia()
    {
        var consultas = new Mock<IRelatorioConsultas>();
        consultas.Setup(c => c.ObterTurmaAsync(10)).ReturnsAsync(new TurmaDoRelatorio(10, "6º Ano A", "Escola A", 2026, "MANHA"));
        var notas = new Mock<INotaService>();
        notas.Setup(n => n.GetMapaDaTurmaAsync(10)).ReturnsAsync(new NotaResult<MapaNotasResponse>(new MapaNotasResponse(
            new RegraAvaliacaoResumoResponse(null, "Padrão do sistema", 10m, 6m, 1, "MEDIA_PONDERADA", true),
            [new NotaDisciplinaResponse(1, "História"), new NotaDisciplinaResponse(2, "Matemática")],
            [
                new MapaNotasAluno("1", "Ana Souza", [7.5m, 8m], 0),
                new MapaNotasAluno("2", "Bruno Lima", [5m, null], 1),
            ])));

        var resultado = await new MapaNotasRelatorio(consultas.Object, notas.Object)
            .GerarAsync(new RelatorioFiltros { TurmaId = 10 }, Hoje);

        Assert.True(resultado.Success, resultado.Message);
        var conteudo = resultado.Value!;
        Assert.Equal(["Nº", "Matrícula", "Aluno", "História", "Matemática", "Abaixo da média"], conteudo.Colunas.Select(c => c.Titulo));
        Assert.Equal(new object?[] { 2, "2", "Bruno Lima", 5m, null, 1 }, conteudo.Linhas[1]);
        Assert.Contains(conteudo.Indicadores, i => i.Rotulo == "Alunos com disciplina abaixo da média" && i.Valor == "1");
        Assert.Equal([6.3m, 8m], conteudo.Grafico!.Series.Single().Valores);
    }

    [Fact]
    public async Task Gerar_SemTurma_RetornaErroDeValidacao()
    {
        var relatorio = new MapaNotasRelatorio(Mock.Of<IRelatorioConsultas>(), Mock.Of<INotaService>());

        var resultado = await relatorio.GerarAsync(new RelatorioFiltros(), Hoje);

        Assert.Equal(RelatorioErro.Validation, resultado.Error);
    }
}
