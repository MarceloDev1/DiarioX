using DiarioX.Server.API.Controllers;
using DiarioX.Server.Application.DTOs.ModalidadesEnsino;
using DiarioX.Server.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DiarioX.Server.Tests.API.Controllers;

public class ModalidadesEnsinoControllerTests
{
    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithModalidades()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        service.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new[] { BuildResponse(1), BuildResponse(2) });

        var result = await new ModalidadesEnsinoController(service.Object).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<ModalidadeEnsinoResponse>>(ok.Value);
        Assert.Equal(2, payload.Count());
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var response = BuildResponse(4);
        service.Setup(s => s.GetByIdAsync(4)).ReturnsAsync(response);

        var result = await new ModalidadesEnsinoController(service.Object).GetById(4);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundWithMessage()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        service.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((ModalidadeEnsinoResponse?)null);

        var result = await new ModalidadesEnsinoController(service.Object).GetById(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Modalidade de ensino nao encontrada.", GetMessage(notFound.Value));
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccess_ReturnsCreatedAtAction()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var request = BuildRequest();
        var response = BuildResponse(20);
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(true, "Modalidade de ensino cadastrada com sucesso.", response));

        var result = await new ModalidadesEnsinoController(service.Object).Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ModalidadesEnsinoController.GetById), created.ActionName);
        Assert.Equal(20, created.RouteValues?["id"]);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task Create_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(false, "Nome obrigatorio.", Error: ModalidadeEnsinoResultError.Validation));

        var result = await new ModalidadesEnsinoController(service.Object).Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Nome obrigatorio.", GetMessage(badRequest.Value));
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(false, "Ja existe uma modalidade de ensino com este nome.", Error: ModalidadeEnsinoResultError.Conflict));

        var result = await new ModalidadesEnsinoController(service.Object).Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Ja existe uma modalidade de ensino com este nome.", GetMessage(conflict.Value));
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOkWithResponse()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var request = BuildRequest();
        var response = BuildResponse(7);
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(true, "Modalidade de ensino atualizada com sucesso.", response));

        var result = await new ModalidadesEnsinoController(service.Object).Update(7, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(50, request))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(false, "Modalidade de ensino nao encontrada.", Error: ModalidadeEnsinoResultError.NotFound));

        var result = await new ModalidadesEnsinoController(service.Object).Update(50, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Modalidade de ensino nao encontrada.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Update_WhenConflict_ReturnsConflict()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(false, "Ja existe uma modalidade de ensino com esta sigla.", Error: ModalidadeEnsinoResultError.Conflict));

        var result = await new ModalidadesEnsinoController(service.Object).Update(7, request);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Ja existe uma modalidade de ensino com esta sigla.", GetMessage(conflict.Value));
    }

    [Fact]
    public async Task Update_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        var request = BuildRequest();
        service.Setup(s => s.UpdateAsync(7, request))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(false, "Status invalido. Valores permitidos: ATIVO ou INATIVO.", Error: ModalidadeEnsinoResultError.Validation));

        var result = await new ModalidadesEnsinoController(service.Object).Update(7, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Status invalido. Valores permitidos: ATIVO ou INATIVO.", GetMessage(badRequest.Value));
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsOkWithMessage()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        service.Setup(s => s.DeleteAsync(3))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(true, "Modalidade de ensino removida com sucesso."));

        var result = await new ModalidadesEnsinoController(service.Object).Delete(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Modalidade de ensino removida com sucesso.", GetMessage(ok.Value));
    }

    [Fact]
    public async Task Delete_WhenNotFound_ReturnsNotFound()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        service.Setup(s => s.DeleteAsync(99))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(false, "Modalidade de ensino nao encontrada.", Error: ModalidadeEnsinoResultError.NotFound));

        var result = await new ModalidadesEnsinoController(service.Object).Delete(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Modalidade de ensino nao encontrada.", GetMessage(notFound.Value));
    }

    [Fact]
    public async Task Delete_WhenValidationError_ReturnsBadRequest()
    {
        var service = new Mock<IModalidadeEnsinoService>();
        service.Setup(s => s.DeleteAsync(2))
            .ReturnsAsync(new ModalidadeEnsinoCommandResult(false, "Erro de validacao.", Error: ModalidadeEnsinoResultError.Validation));

        var result = await new ModalidadesEnsinoController(service.Object).Delete(2);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Erro de validacao.", GetMessage(badRequest.Value));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ModalidadeEnsinoRequest BuildRequest() => new()
    {
        Nome = "Ensino Fundamental",
        Sigla = "EF",
        CodigoMec = "0001",
        Descricao = "Ensino de base.",
        Status = "ATIVO"
    };

    private static ModalidadeEnsinoResponse BuildResponse(int id) => new(
        id,
        "Ensino Fundamental",
        "EF",
        "0001",
        "Ensino de base.",
        "ATIVO"
    );

    private static string? GetMessage(object? value)
    {
        if (value is null) return null;
        return value.GetType().GetProperty("message")?.GetValue(value)?.ToString();
    }
}
