using DiarioX.Server.Application.DTOs.AnosLetivos;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class AnoLetivoServiceTests
{
    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsMappedResponses()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[] { BuildEntity(1), BuildEntity(2) });

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(BuildEntity(3));

        var result = await service.GetByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        Assert.Equal(2026, result.AnoReferencia);
        Assert.Equal(AnoLetivo.TipoBimestral, result.TipoPeriodo);
        Assert.Equal(4, result.Periodos.Count);
        Assert.Equal("1º Bimestre", result.Periodos[0].Nome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((AnoLetivo?)null);

        var result = await service.GetByIdAsync(99);

        Assert.Null(result);
    }

    // ── CreateAsync – validação ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenAnoReferenciaMenorQue2000_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.AnoReferencia = 1999;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenAnoReferenciaMaiorQue2100_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.AnoReferencia = 2101;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenDataInicioIgualTermino_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.DataTermino = request.DataInicio;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenDataInicioMaiorQueTermino_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.DataInicio = new DateOnly(2026, 12, 1);
        request.DataTermino = new DateOnly(2026, 2, 1);

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
        Assert.Contains("início", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenTipoPeriodoInvalido_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.TipoPeriodo = "QUINZENAL";

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenQuantidadePeriodosErrada_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.TipoPeriodo = AnoLetivo.TipoBimestral;
        request.Periodos.RemoveAt(0);

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
        Assert.Contains("4", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenTrimestralComQuantidadeErrada_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.TipoPeriodo = AnoLetivo.TipoTrimestral;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
        Assert.Contains("3", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPeriodoNomeVazio_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.Periodos[0].Nome = "";

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenPeriodoDatasIguais_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.Periodos[1].DataTermino = request.Periodos[1].DataInicio;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
        Assert.Contains("período 2", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPeriodoAntesDaInicioAnoLetivo_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.Periodos[0].DataInicio = new DateOnly(2026, 1, 1);

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
        Assert.Contains("período 1", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPeriodoDepoisDoTerminoAnoLetivo_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.Periodos[3].DataTermino = new DateOnly(2026, 12, 31);

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
        Assert.Contains("período 4", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenAnoReferenciaJaExiste_ReturnsConflict()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.ExistsByAnoReferenciaAsync(2026, null)).ReturnsAsync(true);

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Conflict, result.Error);
        Assert.Contains("2026", result.Message);
    }

    // ── CreateAsync – sucesso ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenValido_CriaERetornaResponse()
    {
        var (service, repo) = BuildService();
        SetupNoConflicts(repo);

        AnoLetivo? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<AnoLetivo>()))
            .Callback<AnoLetivo>(a => captured = a)
            .ReturnsAsync((AnoLetivo a) => { a.Id = 10; return a; });
        repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildEntity(10));

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.True(result.Success);
        Assert.Equal("Ano letivo cadastrado com sucesso.", result.Message);
        Assert.NotNull(result.AnoLetivo);
        Assert.Equal(10, result.AnoLetivo!.Id);

        Assert.NotNull(captured);
        Assert.Equal(2026, captured!.AnoReferencia);
        Assert.Equal(AnoLetivo.TipoBimestral, captured.TipoPeriodo);
        Assert.Equal(4, captured.Periodos.Count);
        Assert.Equal("1º Bimestre", captured.Periodos.First().Nome);
    }

    [Fact]
    public async Task CreateAsync_TrimaNomeDoPeriodo()
    {
        var (service, repo) = BuildService();
        SetupNoConflicts(repo);

        var request = BuildValidRequest();
        request.Periodos[0].Nome = "  1º Bimestre  ";

        AnoLetivo? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<AnoLetivo>()))
            .Callback<AnoLetivo>(a => captured = a)
            .ReturnsAsync((AnoLetivo a) => { a.Id = 1; return a; });
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildEntity(1));

        await service.CreateAsync(request);

        Assert.NotNull(captured);
        Assert.Equal("1º Bimestre", captured!.Periodos.First().Nome);
    }

    [Fact]
    public async Task CreateAsync_AceitaSemestralComDoisPeriodos()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.ExistsByAnoReferenciaAsync(It.IsAny<int>(), null)).ReturnsAsync(false);

        var request = new AnoLetivoRequest
        {
            AnoReferencia = 2026,
            DataInicio = new DateOnly(2026, 2, 1),
            DataTermino = new DateOnly(2026, 12, 15),
            TipoPeriodo = AnoLetivo.TipoSemestral,
            Periodos = new List<PeriodoAvaliativoRequest>
            {
                new() { Nome = "1º Semestre", Numero = 1, DataInicio = new DateOnly(2026, 2, 1), DataTermino = new DateOnly(2026, 6, 30) },
                new() { Nome = "2º Semestre", Numero = 2, DataInicio = new DateOnly(2026, 7, 1), DataTermino = new DateOnly(2026, 12, 15) },
            }
        };

        AnoLetivo? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<AnoLetivo>()))
            .Callback<AnoLetivo>(a => captured = a)
            .ReturnsAsync((AnoLetivo a) => { a.Id = 1; return a; });
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildEntity(1));

        var result = await service.CreateAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Periodos.Count);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync((AnoLetivo?)null);

        var result = await service.UpdateAsync(55, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.NotFound, result.Error);
        Assert.Equal("Ano letivo não encontrado.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenAnoReferenciaConflito_ReturnsConflict()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildEntity(7));
        repo.Setup(r => r.ExistsByAnoReferenciaAsync(2026, 7)).ReturnsAsync(true);

        var result = await service.UpdateAsync(7, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenValido_AtualizaERetornaResponse()
    {
        var (service, repo) = BuildService();
        repo.SetupSequence(r => r.GetByIdAsync(7))
            .ReturnsAsync(BuildEntity(7))
            .ReturnsAsync(BuildEntity(7));
        SetupNoConflicts(repo, excludeId: 7);

        var result = await service.UpdateAsync(7, BuildValidRequest());

        Assert.True(result.Success);
        Assert.Equal("Ano letivo atualizado com sucesso.", result.Message);
        repo.Verify(r => r.UpdateAsync(It.IsAny<AnoLetivo>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDatasInvalidas_ReturnsValidationError()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildEntity(7));

        var request = BuildValidRequest();
        request.DataInicio = new DateOnly(2026, 12, 1);
        request.DataTermino = new DateOnly(2026, 2, 1);

        var result = await service.UpdateAsync(7, request);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.Validation, result.Error);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(200)).ReturnsAsync((AnoLetivo?)null);

        var result = await service.DeleteAsync(200);

        Assert.False(result.Success);
        Assert.Equal(AnoLetivoResultError.NotFound, result.Error);
        Assert.Equal("Ano letivo não encontrado.", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ExcluiERetornaSuccess()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildEntity(5));

        var result = await service.DeleteAsync(5);

        Assert.True(result.Success);
        Assert.Equal("Ano letivo excluído com sucesso.", result.Message);
        repo.Verify(r => r.DeleteAsync(It.Is<AnoLetivo>(a => a.Id == 5)), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (AnoLetivoService Service, Mock<IAnoLetivoRepository> Repo) BuildService()
    {
        var repo = new Mock<IAnoLetivoRepository>();
        return (new AnoLetivoService(repo.Object), repo);
    }

    private static void SetupNoConflicts(Mock<IAnoLetivoRepository> repo, int? excludeId = null)
    {
        repo.Setup(r => r.ExistsByAnoReferenciaAsync(It.IsAny<int>(), excludeId)).ReturnsAsync(false);
    }

    private static AnoLetivoRequest BuildValidRequest() => new()
    {
        AnoReferencia = 2026,
        DataInicio = new DateOnly(2026, 2, 1),
        DataTermino = new DateOnly(2026, 12, 15),
        TipoPeriodo = AnoLetivo.TipoBimestral,
        Periodos = new List<PeriodoAvaliativoRequest>
        {
            new() { Nome = "1º Bimestre", Numero = 1, DataInicio = new DateOnly(2026, 2, 1),  DataTermino = new DateOnly(2026, 4, 30) },
            new() { Nome = "2º Bimestre", Numero = 2, DataInicio = new DateOnly(2026, 5, 1),  DataTermino = new DateOnly(2026, 7, 15) },
            new() { Nome = "3º Bimestre", Numero = 3, DataInicio = new DateOnly(2026, 7, 16), DataTermino = new DateOnly(2026, 9, 30) },
            new() { Nome = "4º Bimestre", Numero = 4, DataInicio = new DateOnly(2026, 10, 1), DataTermino = new DateOnly(2026, 12, 15) },
        }
    };

    private static AnoLetivo BuildEntity(int id) => new()
    {
        Id = id,
        AnoReferencia = 2026,
        DataInicio = new DateOnly(2026, 2, 1),
        DataTermino = new DateOnly(2026, 12, 15),
        TipoPeriodo = AnoLetivo.TipoBimestral,
        Periodos = new List<PeriodoAvaliativo>
        {
            new() { Id = 1, AnoLetivoId = id, Nome = "1º Bimestre", Numero = 1, DataInicio = new DateOnly(2026, 2, 1),  DataTermino = new DateOnly(2026, 4, 30) },
            new() { Id = 2, AnoLetivoId = id, Nome = "2º Bimestre", Numero = 2, DataInicio = new DateOnly(2026, 5, 1),  DataTermino = new DateOnly(2026, 7, 15) },
            new() { Id = 3, AnoLetivoId = id, Nome = "3º Bimestre", Numero = 3, DataInicio = new DateOnly(2026, 7, 16), DataTermino = new DateOnly(2026, 9, 30) },
            new() { Id = 4, AnoLetivoId = id, Nome = "4º Bimestre", Numero = 4, DataInicio = new DateOnly(2026, 10, 1), DataTermino = new DateOnly(2026, 12, 15) },
        }
    };
}
