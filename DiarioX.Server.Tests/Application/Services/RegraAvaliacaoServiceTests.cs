using DiarioX.Server.Application.DTOs.RegrasAvaliacao;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;
using Xunit;

namespace DiarioX.Server.Tests.Application.Services;

public class RegraAvaliacaoServiceTests
{
    private readonly Mock<IRegraAvaliacaoRepository> _repository = new();
    private readonly Mock<IEtapaEnsinoRepository> _etapas = new();
    private readonly RegraAvaliacaoService _service;

    public RegraAvaliacaoServiceTests()
    {
        _etapas.Setup(r => r.GetAllAsync()).ReturnsAsync([new EtapaEnsino { Id = 1 }, new EtapaEnsino { Id = 2 }]);
        _service = new RegraAvaliacaoService(_repository.Object, _etapas.Object);
    }

    private static RegraAvaliacaoRequest Request() => new()
    {
        Nome = " Fundamental II ",
        NotaMaxima = 10m,
        MediaAprovacao = 7m,
        CasasDecimais = 1,
        CalculoNotaPeriodo = "media_ponderada",
        PermiteRecuperacao = true,
        EtapaEnsinoIds = [1, 2, 2],
    };

    [Fact]
    public async Task Create_Valida_GravaNormalizadaComAsEtapas()
    {
        RegraAvaliacao? gravada = null;
        IReadOnlyCollection<int>? etapas = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<RegraAvaliacao>(), It.IsAny<IReadOnlyCollection<int>>()))
            .Callback<RegraAvaliacao, IReadOnlyCollection<int>>((r, e) => { gravada = r; etapas = e; })
            .ReturnsAsync((RegraAvaliacao r, IReadOnlyCollection<int> _) => { r.Id = 9; return r; });
        _repository.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(() => gravada);

        var result = await _service.CreateAsync(Request());

        Assert.True(result.Success, result.Message);
        Assert.Equal(("Fundamental II", RegraAvaliacao.CalculoMediaPonderada), (gravada!.Nome, gravada.CalculoNotaPeriodo));
        Assert.Equal([1, 2], etapas);
    }

    [Theory]
    [InlineData(10, 11, 1, "MEDIA_PONDERADA", "média para aprovação")]
    [InlineData(0, 6, 1, "MEDIA_PONDERADA", "nota máxima")]
    [InlineData(10, 6, 3, "MEDIA_PONDERADA", "casas decimais")]
    [InlineData(10, 6, 1, "CONCEITO", "Forma de cálculo inválida")]
    public async Task Create_DadosInvalidos_RetornaErro(int notaMaxima, int media, int casas, string calculo, string trecho)
    {
        var request = Request();
        request.NotaMaxima = notaMaxima;
        request.MediaAprovacao = media;
        request.CasasDecimais = casas;
        request.CalculoNotaPeriodo = calculo;

        var result = await _service.CreateAsync(request);

        Assert.Equal(RegraAvaliacaoResultError.Validation, result.Error);
        Assert.Contains(trecho, result.Message);
    }

    [Fact]
    public async Task Create_NomeDuplicado_RetornaConflito()
    {
        _repository.Setup(r => r.ExistsByNomeAsync("Fundamental II", null)).ReturnsAsync(true);

        var result = await _service.CreateAsync(Request());

        Assert.Equal(RegraAvaliacaoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task Create_EtapaInexistente_RetornaNotFound()
    {
        var request = Request();
        request.EtapaEnsinoIds = [1, 99];

        var result = await _service.CreateAsync(request);

        Assert.Equal(RegraAvaliacaoResultError.NotFound, result.Error);
        _repository.Verify(r => r.AddAsync(It.IsAny<RegraAvaliacao>(), It.IsAny<IReadOnlyCollection<int>>()), Times.Never);
    }

    [Fact]
    public async Task Update_RegraInexistente_RetornaNotFound()
    {
        var result = await _service.UpdateAsync(5, Request());

        Assert.Equal(RegraAvaliacaoResultError.NotFound, result.Error);
    }

    [Fact]
    public void GetPadrao_DevolveARegraDoSistemaSemId()
    {
        var padrao = _service.GetPadrao();

        Assert.Null(padrao.Id);
        Assert.Equal((10m, 6m, RegraAvaliacao.CalculoMediaPonderada), (padrao.NotaMaxima, padrao.MediaAprovacao, padrao.CalculoNotaPeriodo));
    }
}
