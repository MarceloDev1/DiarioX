using DiarioX.Server.Application.DTOs.EtapasEnsino;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class EtapaEnsinoServiceTests
{
    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsMappedResponses()
    {
        var (service, repo, _) = BuildService();
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
        var (service, repo, _) = BuildService();
        repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(BuildEntity(3));

        var result = await service.GetByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        Assert.Equal(1, result.ModalidadeEnsinoId);
        Assert.Equal("Ensino Fundamental", result.ModalidadeEnsinoNome);
        Assert.Equal("3º Ano", result.Nome);
        Assert.Equal("3ANO", result.Sigla);
        Assert.Equal(3, result.OrdemCronologica);
        Assert.Equal(8, result.IdadeRecomendada);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var (service, repo, _) = BuildService();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((EtapaEnsino?)null);

        var result = await service.GetByIdAsync(99);

        Assert.Null(result);
    }

    // ── CreateAsync – validação ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenModalidadeMissing_ReturnsValidationError()
    {
        var (service, _, _) = BuildService();
        var request = BuildValidRequest();
        request.ModalidadeEnsinoId = 0;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Validation, result.Error);
        Assert.Equal("Modalidade de ensino obrigatória.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenNomeMissing_ReturnsValidationError()
    {
        var (service, _, _) = BuildService();
        var request = BuildValidRequest();
        request.Nome = "";

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Validation, result.Error);
        Assert.Equal("Nome obrigatório.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSiglaMissing_ReturnsValidationError()
    {
        var (service, _, _) = BuildService();
        var request = BuildValidRequest();
        request.Sigla = "   ";

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Validation, result.Error);
        Assert.Equal("Sigla obrigatória.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenOrdemZero_ReturnsValidationError()
    {
        var (service, _, _) = BuildService();
        var request = BuildValidRequest();
        request.OrdemCronologica = 0;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Validation, result.Error);
        Assert.Equal("Ordem cronológica deve ser maior que zero.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenIdadeZero_ReturnsValidationError()
    {
        var (service, _, _) = BuildService();
        var request = BuildValidRequest();
        request.IdadeRecomendada = 0;

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Validation, result.Error);
        Assert.Equal("Idade recomendada deve ser maior que zero.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenModalidadeNotFound_ReturnsNotFound()
    {
        var (service, _, modalidadeRepo) = BuildService();
        modalidadeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ModalidadeEnsino?)null);

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.NotFound, result.Error);
        Assert.Equal("Modalidade de ensino não encontrada.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenNomeInModalidadeAlreadyExists_ReturnsConflict()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        SetupModalidadeExists(modalidadeRepo);
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, null)).ReturnsAsync(true);

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Conflict, result.Error);
        Assert.Equal("Já existe uma etapa com este nome nesta modalidade.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSiglaAlreadyExists_ReturnsConflict()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        SetupModalidadeExists(modalidadeRepo);
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("3ANO", null)).ReturnsAsync(true);

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Conflict, result.Error);
        Assert.Equal("Já existe uma etapa com esta sigla.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenOrdemInModalidadeAlreadyExists_ReturnsConflict()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        SetupModalidadeExists(modalidadeRepo);
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("3ANO", null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsByOrdemInModalidadeAsync(3, 1, null)).ReturnsAsync(true);

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Conflict, result.Error);
        Assert.Equal("Já existe uma etapa com esta ordem cronológica nesta modalidade.", result.Message);
    }

    // ── CreateAsync – sucesso ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesAndReturnsResponse()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        SetupModalidadeExists(modalidadeRepo);
        SetupNoConflicts(repo);

        EtapaEnsino? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<EtapaEnsino>()))
            .Callback<EtapaEnsino>(e => captured = e)
            .ReturnsAsync((EtapaEnsino e) => { e.Id = 10; return e; });
        repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildEntity(10));

        var result = await service.CreateAsync(BuildValidRequest());

        Assert.True(result.Success);
        Assert.Equal("Etapa de ensino cadastrada com sucesso.", result.Message);
        Assert.NotNull(result.EtapaEnsino);
        Assert.Equal(10, result.EtapaEnsino!.Id);

        Assert.NotNull(captured);
        Assert.Equal(1, captured!.ModalidadeEnsinoId);
        Assert.Equal("3º Ano", captured.Nome);
        Assert.Equal("3ANO", captured.Sigla);
        Assert.Equal(3, captured.OrdemCronologica);
        Assert.Equal(8, captured.IdadeRecomendada);
    }

    [Fact]
    public async Task CreateAsync_NormalizesSiglaToUpperCase()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        SetupModalidadeExists(modalidadeRepo);

        var request = BuildValidRequest();
        request.Sigla = "  3ano  ";
        request.Nome = "  3º Ano  ";

        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("3ANO", null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsByOrdemInModalidadeAsync(3, 1, null)).ReturnsAsync(false);

        EtapaEnsino? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<EtapaEnsino>()))
            .Callback<EtapaEnsino>(e => captured = e)
            .ReturnsAsync((EtapaEnsino e) => { e.Id = 1; return e; });
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildEntity(1));

        await service.CreateAsync(request);

        Assert.NotNull(captured);
        Assert.Equal("3ANO", captured!.Sigla);
        Assert.Equal("3º Ano", captured.Nome);
    }

    [Fact]
    public async Task CreateAsync_WhenIdadeRecomendadaNull_SetsToNull()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        SetupModalidadeExists(modalidadeRepo);
        SetupNoConflicts(repo);

        var request = BuildValidRequest();
        request.IdadeRecomendada = null;

        EtapaEnsino? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<EtapaEnsino>()))
            .Callback<EtapaEnsino>(e => captured = e)
            .ReturnsAsync((EtapaEnsino e) => { e.Id = 1; return e; });
        repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildEntity(1));

        await service.CreateAsync(request);

        Assert.NotNull(captured);
        Assert.Null(captured!.IdadeRecomendada);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        var (service, repo, _) = BuildService();
        repo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync((EtapaEnsino?)null);

        var result = await service.UpdateAsync(55, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.NotFound, result.Error);
        Assert.Equal("Etapa de ensino não encontrada.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenNomeConflict_ReturnsConflict()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildEntity(7));
        SetupModalidadeExists(modalidadeRepo);
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, 7)).ReturnsAsync(true);

        var result = await service.UpdateAsync(7, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenSiglaConflict_ReturnsConflict()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildEntity(7));
        SetupModalidadeExists(modalidadeRepo);
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, 7)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("3ANO", 7)).ReturnsAsync(true);

        var result = await service.UpdateAsync(7, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenOrdemConflict_ReturnsConflict()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildEntity(7));
        SetupModalidadeExists(modalidadeRepo);
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, 7)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("3ANO", 7)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsByOrdemInModalidadeAsync(3, 1, 7)).ReturnsAsync(true);

        var result = await service.UpdateAsync(7, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesFieldsAndReturnsResponse()
    {
        var (service, repo, modalidadeRepo) = BuildService();
        repo.SetupSequence(r => r.GetByIdAsync(7))
            .ReturnsAsync(BuildEntity(7))
            .ReturnsAsync(BuildEntity(7));
        SetupModalidadeExists(modalidadeRepo);
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync("3º Ano", 1, 7)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("3ANO", 7)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsByOrdemInModalidadeAsync(3, 1, 7)).ReturnsAsync(false);

        var request = new EtapaEnsinoRequest
        {
            ModalidadeEnsinoId = 1,
            Nome = "3º Ano",
            Sigla = "3ANO",
            OrdemCronologica = 3,
            IdadeRecomendada = 9
        };

        var result = await service.UpdateAsync(7, request);

        Assert.True(result.Success);
        Assert.Equal("Etapa de ensino atualizada com sucesso.", result.Message);
        repo.Verify(r => r.UpdateAsync(It.IsAny<EtapaEnsino>()), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        var (service, repo, _) = BuildService();
        repo.Setup(r => r.GetByIdAsync(200)).ReturnsAsync((EtapaEnsino?)null);

        var result = await service.DeleteAsync(200);

        Assert.False(result.Success);
        Assert.Equal(EtapaEnsinoResultError.NotFound, result.Error);
        Assert.Equal("Etapa de ensino não encontrada.", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsSuccess()
    {
        var (service, repo, _) = BuildService();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildEntity(5));

        var result = await service.DeleteAsync(5);

        Assert.True(result.Success);
        Assert.Equal("Etapa de ensino removida com sucesso.", result.Message);
        repo.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (EtapaEnsinoService Service, Mock<IEtapaEnsinoRepository> Repo, Mock<IModalidadeEnsinoRepository> ModalidadeRepo) BuildService()
    {
        var repo = new Mock<IEtapaEnsinoRepository>();
        var modalidadeRepo = new Mock<IModalidadeEnsinoRepository>();
        return (new EtapaEnsinoService(repo.Object, modalidadeRepo.Object), repo, modalidadeRepo);
    }

    private static void SetupModalidadeExists(Mock<IModalidadeEnsinoRepository> modalidadeRepo)
    {
        modalidadeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ModalidadeEnsino
        {
            Id = 1,
            Nome = "Ensino Fundamental",
            Sigla = "EF",
            Status = ModalidadeEnsino.StatusAtivo
        });
    }

    private static void SetupNoConflicts(Mock<IEtapaEnsinoRepository> repo, int? excludeId = null)
    {
        repo.Setup(r => r.ExistsByNomeInModalidadeAsync(It.IsAny<string>(), It.IsAny<int>(), excludeId)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync(It.IsAny<string>(), excludeId)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsByOrdemInModalidadeAsync(It.IsAny<int>(), It.IsAny<int>(), excludeId)).ReturnsAsync(false);
    }

    private static EtapaEnsinoRequest BuildValidRequest() => new()
    {
        ModalidadeEnsinoId = 1,
        Nome = "3º Ano",
        Sigla = "3ANO",
        OrdemCronologica = 3,
        IdadeRecomendada = 8
    };

    private static EtapaEnsino BuildEntity(int id) => new()
    {
        Id = id,
        ModalidadeEnsinoId = 1,
        ModalidadeEnsino = new ModalidadeEnsino
        {
            Id = 1,
            Nome = "Ensino Fundamental",
            Sigla = "EF",
            Status = ModalidadeEnsino.StatusAtivo
        },
        Nome = "3º Ano",
        Sigla = "3ANO",
        OrdemCronologica = 3,
        IdadeRecomendada = 8
    };
}
