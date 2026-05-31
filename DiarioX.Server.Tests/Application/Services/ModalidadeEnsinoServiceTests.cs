using DiarioX.Server.Application.DTOs.ModalidadesEnsino;
using DiarioX.Server.Application.Services;
using DiarioX.Server.Domain.Entities;
using DiarioX.Server.Domain.Interfaces;
using Moq;

namespace DiarioX.Server.Tests.Application.Services;

public class ModalidadeEnsinoServiceTests
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
        Assert.Equal("Ensino Fundamental", result.Nome);
        Assert.Equal("EF", result.Sigla);
        Assert.Equal("0001", result.CodigoMec);
        Assert.Equal("Ensino de base.", result.Descricao);
        Assert.Equal(ModalidadeEnsino.StatusAtivo, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ModalidadeEnsino?)null);

        var result = await service.GetByIdAsync(99);

        Assert.Null(result);
    }

    // ── CreateAsync – validação ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenNomeMissing_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.Nome = "";

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.Validation, result.Error);
        Assert.Equal("Nome obrigatorio.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSiglaMissing_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.Sigla = "   ";

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.Validation, result.Error);
        Assert.Equal("Sigla obrigatoria.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenStatusInvalid_ReturnsValidationError()
    {
        var (service, _) = BuildService();
        var request = BuildValidRequest();
        request.Status = "SUSPENSO";

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.Validation, result.Error);
        Assert.Equal("Status invalido. Valores permitidos: ATIVO ou INATIVO.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenNomeAlreadyExists_ReturnsConflict()
    {
        var (service, repo) = BuildService();
        var request = BuildValidRequest();

        repo.Setup(r => r.ExistsByNomeAsync("Ensino Fundamental", null)).ReturnsAsync(true);

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.Conflict, result.Error);
        Assert.Equal("Ja existe uma modalidade de ensino com este nome.", result.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenSiglaAlreadyExists_ReturnsConflict()
    {
        var (service, repo) = BuildService();
        var request = BuildValidRequest();

        repo.Setup(r => r.ExistsByNomeAsync("Ensino Fundamental", null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("EF", null)).ReturnsAsync(true);

        var result = await service.CreateAsync(request);

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.Conflict, result.Error);
        Assert.Equal("Ja existe uma modalidade de ensino com esta sigla.", result.Message);
    }

    // ── CreateAsync – sucesso ────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesAndReturnsResponse()
    {
        var (service, repo) = BuildService();
        var request = BuildValidRequest();

        repo.Setup(r => r.ExistsByNomeAsync("Ensino Fundamental", null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("EF", null)).ReturnsAsync(false);

        ModalidadeEnsino? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ModalidadeEnsino>()))
            .Callback<ModalidadeEnsino>(m => captured = m)
            .ReturnsAsync((ModalidadeEnsino m) => { m.Id = 10; return m; });

        var result = await service.CreateAsync(request);

        Assert.True(result.Success);
        Assert.Equal("Modalidade de ensino cadastrada com sucesso.", result.Message);
        Assert.NotNull(result.ModalidadeEnsino);
        Assert.Equal(10, result.ModalidadeEnsino!.Id);

        Assert.NotNull(captured);
        Assert.Equal("Ensino Fundamental", captured!.Nome);
        Assert.Equal("EF", captured.Sigla);
        Assert.Equal("0001", captured.CodigoMec);
        Assert.Equal("Ensino de base.", captured.Descricao);
        Assert.Equal(ModalidadeEnsino.StatusAtivo, captured.Status);
    }

    [Fact]
    public async Task CreateAsync_NormalizesRequest()
    {
        var (service, repo) = BuildService();
        var request = new ModalidadeEnsinoRequest
        {
            Nome = "  Ensino Fundamental  ",
            Sigla = "  ef  ",
            CodigoMec = "  0001  ",
            Descricao = "  Ensino de base.  ",
            Status = "  ativo  "
        };

        repo.Setup(r => r.ExistsByNomeAsync("Ensino Fundamental", null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("EF", null)).ReturnsAsync(false);

        ModalidadeEnsino? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ModalidadeEnsino>()))
            .Callback<ModalidadeEnsino>(m => captured = m)
            .ReturnsAsync((ModalidadeEnsino m) => m);

        await service.CreateAsync(request);

        Assert.NotNull(captured);
        Assert.Equal("Ensino Fundamental", captured!.Nome);
        Assert.Equal("EF", captured.Sigla);
        Assert.Equal("0001", captured.CodigoMec);
        Assert.Equal("Ensino de base.", captured.Descricao);
        Assert.Equal("ATIVO", captured.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenCodigoMecEmpty_SetsToNull()
    {
        var (service, repo) = BuildService();
        var request = BuildValidRequest();
        request.CodigoMec = "   ";

        repo.Setup(r => r.ExistsByNomeAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync(It.IsAny<string>(), null)).ReturnsAsync(false);

        ModalidadeEnsino? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<ModalidadeEnsino>()))
            .Callback<ModalidadeEnsino>(m => captured = m)
            .ReturnsAsync((ModalidadeEnsino m) => m);

        await service.CreateAsync(request);

        Assert.NotNull(captured);
        Assert.Null(captured!.CodigoMec);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(55)).ReturnsAsync((ModalidadeEnsino?)null);

        var result = await service.UpdateAsync(55, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.NotFound, result.Error);
        Assert.Equal("Modalidade de ensino nao encontrada.", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenNomeConflict_ReturnsConflict()
    {
        var (service, repo) = BuildService();
        var existing = BuildEntity(7);
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existing);
        repo.Setup(r => r.ExistsByNomeAsync("Ensino Fundamental", 7)).ReturnsAsync(true);

        var result = await service.UpdateAsync(7, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenSiglaConflict_ReturnsConflict()
    {
        var (service, repo) = BuildService();
        var existing = BuildEntity(7);
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existing);
        repo.Setup(r => r.ExistsByNomeAsync("Ensino Fundamental", 7)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("EF", 7)).ReturnsAsync(true);

        var result = await service.UpdateAsync(7, BuildValidRequest());

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.Conflict, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesFieldsAndReturnsResponse()
    {
        var (service, repo) = BuildService();
        var existing = BuildEntity(7);
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existing);
        repo.Setup(r => r.ExistsByNomeAsync("Ensino Fundamental", 7)).ReturnsAsync(false);
        repo.Setup(r => r.ExistsBySiglaAsync("EF", 7)).ReturnsAsync(false);
        repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existing);

        var request = new ModalidadeEnsinoRequest
        {
            Nome = "Ensino Fundamental",
            Sigla = "EF",
            CodigoMec = "9999",
            Descricao = "Nova descricao.",
            Status = "INATIVO"
        };

        var result = await service.UpdateAsync(7, request);

        Assert.True(result.Success);
        Assert.Equal("Modalidade de ensino atualizada com sucesso.", result.Message);
        Assert.Equal("Nova descricao.", existing.Descricao);
        Assert.Equal(ModalidadeEnsino.StatusInativo, existing.Status);
        Assert.Equal("9999", existing.CodigoMec);
        repo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(200)).ReturnsAsync((ModalidadeEnsino?)null);

        var result = await service.DeleteAsync(200);

        Assert.False(result.Success);
        Assert.Equal(ModalidadeEnsinoResultError.NotFound, result.Error);
        Assert.Equal("Modalidade de ensino nao encontrada.", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsSuccess()
    {
        var (service, repo) = BuildService();
        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildEntity(5));

        var result = await service.DeleteAsync(5);

        Assert.True(result.Success);
        Assert.Equal("Modalidade de ensino removida com sucesso.", result.Message);
        repo.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (ModalidadeEnsinoService Service, Mock<IModalidadeEnsinoRepository> Repo) BuildService()
    {
        var repo = new Mock<IModalidadeEnsinoRepository>();
        return (new ModalidadeEnsinoService(repo.Object), repo);
    }

    private static ModalidadeEnsinoRequest BuildValidRequest() => new()
    {
        Nome = "Ensino Fundamental",
        Sigla = "EF",
        CodigoMec = "0001",
        Descricao = "Ensino de base.",
        Status = "ATIVO"
    };

    private static ModalidadeEnsino BuildEntity(int id) => new()
    {
        Id = id,
        Nome = "Ensino Fundamental",
        Sigla = "EF",
        CodigoMec = "0001",
        Descricao = "Ensino de base.",
        Status = ModalidadeEnsino.StatusAtivo
    };
}
