using DiarioX.Server.Application.Notas;
using DiarioX.Server.Domain.Entities;
using Xunit;

namespace DiarioX.Server.Tests.Application.Notas;

public class CalculoNotasTests
{
    private static RegraAvaliacao Media(bool recuperacao = true, int casas = 1) => new()
    {
        NotaMaxima = 10m, MediaAprovacao = 6m, CasasDecimais = casas,
        CalculoNotaPeriodo = RegraAvaliacao.CalculoMediaPonderada, PermiteRecuperacao = recuperacao,
    };

    private static RegraAvaliacao Soma() => new()
    {
        NotaMaxima = 10m, MediaAprovacao = 6m, CasasDecimais = 1,
        CalculoNotaPeriodo = RegraAvaliacao.CalculoSoma, PermiteRecuperacao = true,
    };

    [Fact]
    public void Periodo_MediaPonderadaPelosPesos()
    {
        // (8 × 2 + 5 × 1) / 3 = 7
        var resultado = CalculoNotas.CalcularPeriodo(Media(), [new(2m, 8m, false), new(1m, 5m, false)]);

        Assert.Equal(7m, resultado.Media);
        Assert.Equal(7m, resultado.Nota);
        Assert.Equal(0, resultado.Pendentes);
    }

    [Fact]
    public void Periodo_ConsideraSoAsAvaliacoesLancadas_EContaPendentes()
    {
        var resultado = CalculoNotas.CalcularPeriodo(Media(), [new(1m, 9m, false), new(1m, null, false), new(1m, null, false)]);

        Assert.Equal(9m, resultado.Nota);
        Assert.Equal(2, resultado.Pendentes);
    }

    [Fact]
    public void Periodo_SemNotas_RetornaNulo()
    {
        var resultado = CalculoNotas.CalcularPeriodo(Media(), [new(1m, null, false)]);

        Assert.Null(resultado.Media);
        Assert.Null(resultado.Nota);
        Assert.Equal(1, resultado.Pendentes);
    }

    [Fact]
    public void Periodo_ArredondaNoCasasDaRegra()
    {
        // (7 + 8 + 8) / 3 = 7,666... → 7,7 (1 casa) e 8 (0 casas)
        ItemNotaPeriodo[] itens = [new(1m, 7m, false), new(1m, 8m, false), new(1m, 8m, false)];

        Assert.Equal(7.7m, CalculoNotas.CalcularPeriodo(Media(casas: 1), itens).Nota);
        Assert.Equal(8m, CalculoNotas.CalcularPeriodo(Media(casas: 0), itens).Nota);
    }

    [Fact]
    public void Periodo_RecuperacaoSubstituiQuandoMaior()
    {
        var resultado = CalculoNotas.CalcularPeriodo(Media(), [new(1m, 4m, false), new(1m, 7m, true)]);

        Assert.Equal(4m, resultado.Media);
        Assert.Equal(7m, resultado.Recuperacao);
        Assert.Equal(7m, resultado.Nota);
    }

    [Fact]
    public void Periodo_RecuperacaoMenorNaoReduzANota()
    {
        var resultado = CalculoNotas.CalcularPeriodo(Media(), [new(1m, 5m, false), new(1m, 3m, true)]);

        Assert.Equal(5m, resultado.Nota);
    }

    [Fact]
    public void Periodo_RegraSemRecuperacao_IgnoraANota()
    {
        var resultado = CalculoNotas.CalcularPeriodo(Media(recuperacao: false), [new(1m, 4m, false), new(1m, 9m, true)]);

        Assert.Null(resultado.Recuperacao);
        Assert.Equal(4m, resultado.Nota);
    }

    [Fact]
    public void Periodo_Soma_SomaOsPontosAteANotaMaxima()
    {
        Assert.Equal(8.5m, CalculoNotas.CalcularPeriodo(Soma(), [new(1m, 5.5m, false), new(1m, 3m, false)]).Nota);
        Assert.Equal(10m, CalculoNotas.CalcularPeriodo(Soma(), [new(1m, 7m, false), new(1m, 6m, false)]).Nota);
    }

    [Fact]
    public void Final_MediaDosPeriodosCompleta()
    {
        var aprovado = CalculoNotas.CalcularFinal(Media(), [7m, 6m, 5m, 8m]);
        var abaixo = CalculoNotas.CalcularFinal(Media(), [5m, 6m, 5m, 5.5m]);

        Assert.Equal((6.5m, true, CalculoNotas.SituacaoMediaAtingida), (aprovado.Media, aprovado.Completa, aprovado.Situacao));
        Assert.Equal(CalculoNotas.SituacaoAbaixoDaMedia, abaixo.Situacao);
    }

    [Fact]
    public void Final_ComPeriodoSemNota_FicaEmAndamentoComMediaParcial()
    {
        var resultado = CalculoNotas.CalcularFinal(Media(), [8m, 5m, null, null]);

        Assert.Equal(6.5m, resultado.Media);
        Assert.False(resultado.Completa);
        Assert.Equal(CalculoNotas.SituacaoEmAndamento, resultado.Situacao);
    }

    [Fact]
    public void Final_SemNenhumaNota()
    {
        var resultado = CalculoNotas.CalcularFinal(Media(), [null, null]);

        Assert.Null(resultado.Media);
        Assert.Equal(CalculoNotas.SituacaoSemNotas, resultado.Situacao);
    }

    [Fact]
    public void AbaixoDaMedia_ComparaComAMediaDaRegra()
    {
        Assert.True(CalculoNotas.AbaixoDaMedia(Media(), 5.9m));
        Assert.False(CalculoNotas.AbaixoDaMedia(Media(), 6m));
        Assert.False(CalculoNotas.AbaixoDaMedia(Media(), null));
    }
}
