using DiarioX.Server.API.Controllers;
using DiarioX.Server.Application.DTOs.EtapasEnsino;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class EtapasEnsinoControllerTests
{
    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithEtapas()
    {
        var service = new Mock<IEtapaEnsinoService>();
        service.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new[] { BuildResponse(1), BuildResponse(2) });

        var result = await new EtapasEnsinoController(service.Object).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<EtapaEnsinoResponse>>(ok.Value);
        Assert.Equal(2, payload.Count());
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var response = BuildResponse(4);
        service.Setup(s => s.GetByIdAsync(4)).ReturnsAsync(response);

        var result = await new EtapasEnsinoController(service.Object).GetById(4);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundWithMessage()
    {
        var service = new Mock<IEtapaEnsinoService>();
        service.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((EtapaEnsinoResponse?)null);

        var result = await new EtapasEnsinoController(service.Object).GetById(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Etapa de ensino não encontrada.", GetMessage(notFound.Value));
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        var response = BuildResponse(20);
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(true, "Etapa de ensino cadastrada com sucesso.", response));

        var result = await new EtapasEnsinoController(service.Object).Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(EtapasEnsinoController.GetById), created.ActionName);
        Assert.Equal(20, created.RouteValues?["id"]);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task Create_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Nome obrigatório.", Error: EtapaEnsinoResultError.Validation));

        var result = await new EtapasEnsinoController(service.Object).Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Nome obrigatório.", GetMessage(badRequest.Value));
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Já existe uma etapa com esta sigla.", Error: EtapaEnsinoResultError.Conflict));

        var result = await new EtapasEnsinoController(service.Object).Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Já existe uma etapa com esta sigla.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task Create_WhenModalidadeNotFound_ReturnsNotFound()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Modalidade de ensino não encontrada.", Error: EtapaEnsinoResultError.NotFound));

        var result = await new EtapasEnsinoController(service.Object).Create(request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Modalidade de ensino não encontrada.", GetMessage(notFound.Value));
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOkWithResponse()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        var response = BuildResponse(7);
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(true, "Etapa de ensino atualizada com sucesso.", response));

        var result = await new EtapasEnsinoController(service.Object).Update(7, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(50, request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Etapa de ensino não encontrada.", Error: EtapaEnsinoResultError.NotFound));

        var result = await new EtapasEnsinoController(service.Object).Update(50, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Etapa de ensino não encontrada.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Update_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Já existe uma etapa com esta ordem cronológica nesta modalidade.", Error: EtapaEnsinoResultError.Conflict));

        var result = await new EtapasEnsinoController(service.Object).Update(7, request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Já existe uma etapa com esta ordem cronológica nesta modalidade.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task Update_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IEtapaEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Ordem cronológica deve ser maior que zero.", Error: EtapaEnsinoResultError.Validation));

        var result = await new EtapasEnsinoController(service.Object).Update(7, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Ordem cronológica deve ser maior que zero.", GetMessage(badRequest.Value));
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsOkWithMessage()
    {
        var service = new Mock<IEtapaEnsinoService>();
        service.Setup(s => s.DeleteAsync(3))
            .ReturnsAsync(new EtapaEnsinoCommandResult(true, "Etapa de ensino removida com sucesso."));

        var result = await new EtapasEnsinoController(service.Object).Delete(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Etapa de ensino removida com sucesso.", GetMessage(ok.Value));
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IEtapaEnsinoService>();
        service.Setup(s => s.DeleteAsync(99))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Etapa de ensino não encontrada.", Error: EtapaEnsinoResultError.NotFound));

        var result = await new EtapasEnsinoController(service.Object).Delete(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Etapa de ensino não encontrada.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Delete_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IEtapaEnsinoService>();
        service.Setup(s => s.DeleteAsync(2))
            .ReturnsAsync(new EtapaEnsinoCommandResult(false, "Erro de validação.", Error: EtapaEnsinoResultError.Validation));

        var result = await new EtapasEnsinoController(service.Object).Delete(2);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Erro de validação.", GetMessage(badRequest.Value));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EtapaEnsinoRequest BuildRequest() => new()
    {
        ModalidadeEnsinoId = 1,
        Nome = "3º Ano",
        Sigla = "3ANO",
        OrdemCronologica = 3,
        IdadeRecomendada = 8
    };

    private static EtapaEnsinoResponse BuildResponse(int id) => new(
        id,
        1,
        "Ensino Fundamental",
        "3º Ano",
        "3ANO",
        3,
        8
    );

    private static string? GetMessage(object? value)
    {
        if (value is null) return null;
        return value.GetType().GetProperty("message")?.GetValue(value)?.ToString();
    }
}
